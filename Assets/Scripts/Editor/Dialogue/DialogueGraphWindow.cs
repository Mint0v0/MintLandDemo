using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 对话图编辑器窗口：左栏 GraphView 可视化编辑，右栏编辑选中节点的字段；Save 统一写回资产。
    /// </summary>
    public class DialogueGraphWindow : EditorWindow
    {
        private ObjectField _graphField;
        private DialogueGraphView _graphView;
        private ScrollView _inspectorScroll;
        private ScrollView _validationScroll;
        private List<ValidationIssue> _lastIssues = new List<ValidationIssue>();
        private Vector2 _nextNodePos = new Vector2(100, 100);

        [MenuItem("MintLand/Dialogue Graph Editor")]
        public static void Open()
        {
            GetWindow<DialogueGraphWindow>("Dialogue Graph Editor");
        }

        private void OnEnable()
        {
            BuildUI();
        }

        private void OnDisable()
        {
            // 窗口关闭时清空右栏（下次 OnEnable 会重建，此处兜底）
            if (_inspectorScroll != null)
                _inspectorScroll.Clear();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // 顶部工具栏：资产选择 + Add Node + Save
            var toolbar = new Toolbar();

            _graphField = new ObjectField("Dialogue Graph")
            {
                objectType = typeof(DialogueGraph),
                allowSceneObjects = false
            };
            _graphField.RegisterValueChangedCallback(evt =>
            {
                _graphView.LoadGraph(evt.newValue as DialogueGraph);
                RebuildInspector();
            });
            toolbar.Add(_graphField);

            toolbar.Add(new Button(AddNode) { text = "Add Node" });
            toolbar.Add(new Button(Save) { text = "Save" });
            toolbar.Add(new Button(Validate) { text = "Validate" });
            toolbar.Add(new Button(ExportCsv) { text = "Export CSV" });
            toolbar.Add(new Button(ImportCsv) { text = "Import CSV" });

            rootVisualElement.Add(toolbar);

            // 主体：左 GraphView + 右检查面板（上方校验结果 + 下方节点编辑）
            _graphView = new DialogueGraphView();
            _graphView.style.flexGrow = 1f;

            _inspectorScroll = new ScrollView();
            _inspectorScroll.style.paddingLeft = 8;
            _inspectorScroll.style.paddingRight = 8;
            _inspectorScroll.style.paddingTop = 8;
            _inspectorScroll.style.paddingBottom = 8;
            _inspectorScroll.style.flexGrow = 1f;

            // 校验结果区（固定高度：标题 + 滚动区）
            var validationSection = new VisualElement();
            validationSection.style.height = 220;
            validationSection.style.flexShrink = 0;

            var validationHeader = new Label("校验结果");
            validationHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            validationHeader.style.paddingLeft = 8;
            validationHeader.style.paddingTop = 6;
            validationSection.Add(validationHeader);

            _validationScroll = new ScrollView();
            _validationScroll.style.flexGrow = 1f;
            _validationScroll.style.paddingLeft = 8;
            _validationScroll.style.paddingRight = 8;
            _validationScroll.style.paddingBottom = 8;
            validationSection.Add(_validationScroll);

            // 右栏：上（校验结果）+ 下（节点编辑）
            var rightPane = new VisualElement();
            rightPane.style.flexGrow = 1f;
            rightPane.Add(validationSection);
            rightPane.Add(_inspectorScroll);

            var splitView = new TwoPaneSplitView(1, 320, TwoPaneSplitViewOrientation.Horizontal);
            splitView.Add(_graphView);
            splitView.Add(rightPane);
            splitView.style.flexGrow = 1f;
            rootVisualElement.Add(splitView);

            // 用 UIElements 原生鼠标/键盘事件，延后一帧读选中状态
            _graphView.RegisterCallback<MouseUpEvent>(_ => ScheduleRefresh());
            _graphView.RegisterCallback<KeyUpEvent>(_ => ScheduleRefresh());
            RebuildInspector();
            ShowValidationPlaceholder();
        }

        private void AddNode()
        {
            var data = new DialogueNode { speakerName = "新节点" };
            _graphView.CreateNode(data, _nextNodePos);
            _nextNodePos += new Vector2(40, 40);
        }

        private void Save()
        {
            DialogueGraph graph = _graphField.value as DialogueGraph;
            if (graph == null)
            {
                EditorUtility.DisplayDialog("未选择资产", "请先在顶部选择一个 DialogueGraph 资产。", "OK");
                return;
            }

            _graphView.SaveGraph(graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        private void ExportCsv()
        {
            DialogueGraph graph = _graphField.value as DialogueGraph;
            if (graph == null)
            {
                EditorUtility.DisplayDialog("未选择资产", "请先在顶部选择一个 DialogueGraph 资产。", "OK");
                return;
            }

            string folder = EditorUtility.SaveFolderPanel("选择导出目录", "", "DialogueCsv");
            if (string.IsNullOrEmpty(folder)) return;

            DialogueCsvExporter.Export(graph, folder);
            EditorUtility.DisplayDialog("导出完成", $"已导出到：\n{folder}", "OK");
            Debug.Log($"[DialogueCsv] 导出完成: {folder}");
        }

        private void ImportCsv()
        {
            string folder = EditorUtility.OpenFolderPanel("选择 CSV 目录", "", "");
            if (string.IsNullOrEmpty(folder)) return;

            DialogueGraph imported = DialogueCsvImporter.Import(folder);
            if (imported == null)
            {
                EditorUtility.DisplayDialog("导入失败", "CSV 文件缺失或格式错误，详见 Console。", "OK");
                return;
            }

            // 询问覆盖还是新建
            bool hasExisting = _graphField.value as DialogueGraph != null;
            int choice = hasExisting
                ? EditorUtility.DisplayDialogComplex("导入完成", "如何处理导入的数据？", "覆盖当前资产", "取消", "新建资产")
                : 2;  // 没有现成资产 → 直接新建

            // 取消：销毁临时实例
            if (choice == 1)
            {
                DestroyImmediate(imported);
                return;
            }

            if (choice == 0)
            {
                // 覆盖：把 imported 的数据拷到现有资产，再销毁临时实例
                DialogueGraph target = _graphField.value as DialogueGraph;
                CopyGraphData(imported, target);
                DestroyImmediate(imported);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                _graphView.LoadGraph(target);
                RebuildInspector();
                Debug.Log($"[DialogueCsv] 覆盖当前资产：{AssetDatabase.GetAssetPath(target)}");
            }
            else
            {
                // 新建：弹保存对话框
                string assetPath = EditorUtility.SaveFilePanelInProject(
                    "保存 DialogueGraph",
                    "ImportedDialogue",
                    "asset",
                    "选择保存路径");
                if (string.IsNullOrEmpty(assetPath))
                {
                    DestroyImmediate(imported);  // 用户取消保存
                    return;
                }

                AssetDatabase.CreateAsset(imported, assetPath);
                AssetDatabase.SaveAssets();
                _graphField.value = imported;
                _graphView.LoadGraph(imported);
                RebuildInspector();
                Debug.Log($"[DialogueCsv] 新建资产：{assetPath}");
            }

            EditorUtility.DisplayDialog("导入成功", "已加载到编辑器。", "OK");
        }

        private static void CopyGraphData(DialogueGraph src, DialogueGraph dst)
        {
            dst.graphId = src.graphId;
            dst.entryNodeId = src.entryNodeId;
            dst.nodes = src.nodes;
            dst.edges = src.edges;
        }

        // ---------------- 校验 ----------------

        private void Validate()
        {
            DialogueGraph graph = _graphField.value as DialogueGraph;
            if (graph == null)
            {
                EditorUtility.DisplayDialog("未选择资产", "请先在顶部选择一个 DialogueGraph 资产。", "OK");
                return;
            }

            List<ValidationIssue> issues = RunValidation(graph);
            _lastIssues = issues;
            RebuildValidationPanel(issues);
        }

        private void RebuildValidationPanel(List<ValidationIssue> issues)
        {
            if (_validationScroll == null) return;
            _validationScroll.Clear();

            if (issues.Count == 0)
            {
                var ok = new Label("✓ 无问题");
                ok.style.color = new Color(0.4f, 0.85f, 0.4f, 1f);
                ok.style.unityTextAlign = TextAnchor.MiddleLeft;
                _validationScroll.Add(ok);
                return;
            }

            foreach (ValidationIssue issue in issues)
            {
                bool isError = issue.severity == IssueSeverity.Error;
                var button = new Button(() => OnIssueClicked(issue))
                {
                    text = (isError ? "✗ " : "⚠ ") + issue.message
                };
                button.style.color = isError ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.85f, 0.3f);
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.whiteSpace = WhiteSpace.Normal;
                if (issue.targetView == null)
                    button.SetEnabled(false);
                _validationScroll.Add(button);
            }
        }

        private void OnIssueClicked(ValidationIssue issue)
        {
            if (issue.targetView == null) return;
            _graphView.ClearSelection();
            _graphView.AddToSelection(issue.targetView);
            _graphView.FrameSelection();
            ScheduleRefresh();
        }

        private void ShowValidationPlaceholder()
        {
            if (_validationScroll == null) return;
            _validationScroll.Clear();
            var hint = new Label("点击 Validate 开始检查");
            hint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            hint.style.unityTextAlign = TextAnchor.MiddleLeft;
            _validationScroll.Add(hint);
        }

        private List<ValidationIssue> RunValidation(DialogueGraph graph)
        {
            var issues = new List<ValidationIssue>();

            // 收集当前图中的节点视图与边（编辑期间的实时数据，无需先 Save）
            var nodeViews = new List<DialogueNodeView>();
            foreach (Node node in _graphView.nodes)
                if (node is DialogueNodeView view)
                    nodeViews.Add(view);

            var edgeDataList = new List<DialogueEdge>();
            foreach (Edge edge in _graphView.edges)
                if (edge.userData is DialogueEdge de)
                    edgeDataList.Add(de);

            // nodeId → view 索引
            var viewByNodeId = new Dictionary<string, DialogueNodeView>();
            foreach (DialogueNodeView view in nodeViews)
            {
                string id = view.Data.nodeId;
                if (!string.IsNullOrEmpty(id) && !viewByNodeId.ContainsKey(id))
                    viewByNodeId[id] = view;
            }

            // 入边 / 出边计数
            var inDegree = new Dictionary<string, int>();
            var outDegree = new Dictionary<string, int>();
            foreach (DialogueEdge de in edgeDataList)
            {
                if (!string.IsNullOrEmpty(de.toNodeId))
                    inDegree[de.toNodeId] = inDegree.TryGetValue(de.toNodeId, out int inC) ? inC + 1 : 1;
                if (!string.IsNullOrEmpty(de.fromNodeId))
                    outDegree[de.fromNodeId] = outDegree.TryGetValue(de.fromNodeId, out int outC) ? outC + 1 : 1;
            }

            // 规则 1：入口节点未设置 / 不存在
            if (string.IsNullOrEmpty(graph.entryNodeId))
            {
                issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = "入口节点未设置" });
            }
            else if (!viewByNodeId.ContainsKey(graph.entryNodeId))
            {
                issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"入口节点 '{graph.entryNodeId}' 不存在" });
            }

            // 规则 2：nodeId 为空 / 重复
            var seenIds = new HashSet<string>();
            foreach (DialogueNodeView view in nodeViews)
            {
                string id = view.Data.nodeId;
                if (string.IsNullOrEmpty(id))
                {
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"节点 '{view.Data.speakerName}' 的 nodeId 为空", targetView = view });
                }
                else if (seenIds.Contains(id))
                {
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"nodeId '{id}' 重复", targetView = view });
                }
                else
                {
                    seenIds.Add(id);
                }
            }

            // 规则 3：边引用不存在的节点
            foreach (DialogueEdge de in edgeDataList)
            {
                if (!string.IsNullOrEmpty(de.fromNodeId) && !viewByNodeId.ContainsKey(de.fromNodeId))
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"边 '{de.edgeId}' 引用了不存在的节点 '{de.fromNodeId}'" });
                if (!string.IsNullOrEmpty(de.toNodeId) && !viewByNodeId.ContainsKey(de.toNodeId))
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"边 '{de.edgeId}' 引用了不存在的节点 '{de.toNodeId}'" });
            }

            // 规则 4：无入边且非入口（孤立节点）
            foreach (DialogueNodeView view in nodeViews)
            {
                string id = view.Data.nodeId;
                if (string.IsNullOrEmpty(id)) continue;
                if (id == graph.entryNodeId) continue;
                if (!inDegree.TryGetValue(id, out int deg) || deg == 0)
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = $"节点 '{id}' 无入边且非入口（孤立节点）", targetView = view });
            }

            // 规则 5：非结束节点无出边（对话会卡死）
            foreach (DialogueNodeView view in nodeViews)
            {
                DialogueNode data = view.Data;
                string id = data.nodeId;
                if (string.IsNullOrEmpty(id)) continue;
                if (outDegree.TryGetValue(id, out int deg) && deg > 0) continue;
                if (data.action == DialogueAction.EndTrade) continue;
                if (data.choices != null && data.choices.Count > 0) continue;
                issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = $"节点 '{id}' 无出口且非结束节点（对话会卡死）", targetView = view });
            }

            // 规则 6：action 缺少必要参数
            foreach (DialogueNodeView view in nodeViews)
            {
                DialogueNode data = view.Data;
                string id = data.nodeId;
                if (string.IsNullOrEmpty(id)) continue;
                bool needQuest = data.action == DialogueAction.AcceptQuest || data.action == DialogueAction.CompleteQuest;
                bool needItem = data.action == DialogueAction.GiveItem || data.action == DialogueAction.RemoveItem;
                if (needQuest && string.IsNullOrEmpty(data.questId))
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = $"节点 '{id}' 的 action 缺少 questId", targetView = view });
                if (needItem && string.IsNullOrEmpty(data.itemId))
                    issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = $"节点 '{id}' 的 action 缺少 itemId", targetView = view });
            }

            // 规则 7：choice.nextNodeId 指向不存在的节点
            foreach (DialogueNodeView view in nodeViews)
            {
                DialogueNode data = view.Data;
                if (data.choices == null) continue;
                foreach (DialogueChoice choice in data.choices)
                {
                    if (choice == null) continue;
                    if (!string.IsNullOrEmpty(choice.nextNodeId) && !viewByNodeId.ContainsKey(choice.nextNodeId))
                        issues.Add(new ValidationIssue { severity = IssueSeverity.Error, message = $"节点 '{data.nodeId}' 的选项 '{choice.choiceText}' 跳转目标 '{choice.nextNodeId}' 不存在", targetView = view });
                }
            }

            return issues;
        }

        // ---------------- 右侧检查面板 ----------------

        private void OnSelectionChanged()
        {
            RebuildInspector();
        }

        private void ScheduleRefresh()
        {
            // 下一帧再读，等 GraphView 处理完选中后再刷新 Inspector
            rootVisualElement.schedule.Execute(RebuildInspector).StartingIn(0);
        }

        private void RebuildInspector()
        {
            if (_inspectorScroll == null) return;
            _inspectorScroll.Clear();

            DialogueNodeView view = null;
            int count = 0;
            foreach (ISelectable selectable in _graphView.selection)
            {
                if (selectable is DialogueNodeView nodeView)
                {
                    view = nodeView;
                    count++;
                }
            }

            if (count != 1)
            {
                var hint = new Label("选中一个节点以编辑");
                hint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                hint.style.unityFontStyleAndWeight = FontStyle.Bold;
                _inspectorScroll.Add(hint);
                return;
            }

            BuildInspector(view);
        }

        private void BuildInspector(DialogueNodeView view)
        {
            DialogueNode data = view.Data;

            // speakerName
            var speakerField = new TextField("Speaker Name") { value = data.speakerName };
            speakerField.RegisterValueChangedCallback(evt =>
            {
                data.speakerName = evt.newValue;
                view.RefreshFromData();
            });
            _inspectorScroll.Add(speakerField);
            AddSpacer();

            // dialogueText（多行）
            var textField = new TextField("Dialogue Text") { value = data.dialogueText, multiline = true };
            textField.style.height = 80;
            textField.RegisterValueChangedCallback(evt =>
            {
                data.dialogueText = evt.newValue;
                view.RefreshFromData();
            });
            _inspectorScroll.Add(textField);
            AddSpacer();

            // action
            var actionField = new EnumField("Action", (Enum)data.action);
            actionField.RegisterValueChangedCallback(evt =>
            {
                data.action = (DialogueAction)evt.newValue;
            });
            _inspectorScroll.Add(actionField);
            AddSpacer();

            // questId
            var questField = new TextField("Quest ID") { value = data.questId };
            questField.RegisterValueChangedCallback(evt => { data.questId = evt.newValue; });
            _inspectorScroll.Add(questField);
            AddSpacer();

            // itemId
            var itemField = new TextField("Item ID") { value = data.itemId };
            itemField.RegisterValueChangedCallback(evt => { data.itemId = evt.newValue; });
            _inspectorScroll.Add(itemField);
            AddSpacer();

            // itemCount
            var countField = new IntegerField("Item Count") { value = data.itemCount };
            countField.RegisterValueChangedCallback(evt => { data.itemCount = evt.newValue; });
            _inspectorScroll.Add(countField);

            BuildOutgoingEdgesSection(_inspectorScroll, view);
        }

        private void AddSpacer()
        {
            var spacer = new VisualElement();
            spacer.style.height = 4;
            _inspectorScroll.Add(spacer);
        }

        // ---------------- Outgoing Edges 区块 ----------------

        private void BuildOutgoingEdgesSection(VisualElement parent, DialogueNodeView view)
        {
            // 分隔线
            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            divider.style.marginTop = 10;
            divider.style.marginBottom = 8;
            parent.Add(divider);

            var header = new Label("Outgoing Edges");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(header);

            var hint = new Label("在画布上从本节点 Out 端口拖线到目标节点以创建分支");
            hint.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(hint);

            var edgesContainer = new VisualElement();
            parent.Add(edgesContainer);

            RebuildOutgoingEdges(edgesContainer, view);
        }

        private void RebuildOutgoingEdges(VisualElement container, DialogueNodeView view)
        {
            container.Clear();

            List<DialogueEdge> outgoing = _graphView.GetEdgesForNode(view.Data.nodeId);
            if (outgoing == null || outgoing.Count == 0)
            {
                var empty = new Label("(暂无出边)");
                empty.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                container.Add(empty);
                return;
            }

            for (int i = 0; i < outgoing.Count; i++)
            {
                container.Add(BuildEdgeFoldout(container, view, outgoing[i], i));
            }
        }

        private Foldout BuildEdgeFoldout(VisualElement container, DialogueNodeView view, DialogueEdge edge, int index)
        {
            var foldout = new Foldout();
            foldout.value = false;
            foldout.text = EdgeSummary(edge, index);

            // 标题栏右侧删除按钮
            VisualElement header = foldout.Q<Toggle>();
            if (header == null)
                header = foldout.Q("unity-checkmark")?.parent;
            if (header != null)
            {
                var removeBtn = new Button(() =>
                {
                    _graphView.RemoveEdge(edge);
                    RebuildOutgoingEdges(container, view);
                }) { text = "X" };
                removeBtn.style.marginLeft = 0;
                header.Add(removeBtn);
            }

            // 折叠区
            var content = new VisualElement();
            content.style.paddingLeft = 8;

            var textField = new TextField("choiceText") { value = edge.choiceText };
            textField.RegisterValueChangedCallback(evt =>
            {
                edge.choiceText = evt.newValue;
                foldout.text = EdgeSummary(edge, index);
                _graphView.NotifyEdgeChanged(edge);
            });
            content.Add(textField);

            var sortField = new IntegerField("sortOrder") { value = edge.sortOrder };
            sortField.RegisterValueChangedCallback(evt => { edge.sortOrder = evt.newValue; });
            content.Add(sortField);

            // 目标节点（只读）
            var targetLabel = new Label(EdgeTargetFull(edge));
            targetLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            content.Add(targetLabel);

            var actionField = new EnumField("action", (Enum)edge.action);
            actionField.RegisterValueChangedCallback(evt => { edge.action = (DialogueAction)evt.newValue; });
            content.Add(actionField);

            var questField = new TextField("questId") { value = edge.questId };
            questField.RegisterValueChangedCallback(evt => { edge.questId = evt.newValue; });
            content.Add(questField);

            var itemField = new TextField("itemId") { value = edge.itemId };
            itemField.RegisterValueChangedCallback(evt => { edge.itemId = evt.newValue; });
            content.Add(itemField);

            var countField = new IntegerField("itemCount") { value = edge.itemCount };
            countField.RegisterValueChangedCallback(evt => { edge.itemCount = evt.newValue; });
            content.Add(countField);

            // Conditions（挂在边上的条件列表）
            if (edge.conditions == null)
                edge.conditions = new List<DialogueConditionData>();
            BuildConditionsSection(content, edge.conditions, () => _graphView.NotifyEdgeChanged(edge));

            foldout.Add(content);
            return foldout;
        }

        // 出边标题：Edge N: "choiceText前20字" → 目标名
        private string EdgeSummary(DialogueEdge edge, int index)
        {
            string text = edge.choiceText ?? string.Empty;
            string choiceDisplay;
            if (string.IsNullOrEmpty(text))
            {
                choiceDisplay = "(无选项文字)";
            }
            else
            {
                if (text.Length > 20) text = text.Substring(0, 20) + "…";
                choiceDisplay = "\"" + text + "\"";
            }
            return "Edge " + index + ": " + choiceDisplay + " → " + EdgeTargetShort(edge);
        }

        // 标题里用的目标名
        private string EdgeTargetShort(DialogueEdge edge)
        {
            if (string.IsNullOrEmpty(edge.toNodeId)) return "(未连线)";
            return TargetSpeakerName(edge.toNodeId);
        }

        // 内容区只读目标标签
        private string EdgeTargetFull(DialogueEdge edge)
        {
            if (string.IsNullOrEmpty(edge.toNodeId)) return "→ (未连线，请在画布上拖线)";
            return "→ " + TargetSpeakerName(edge.toNodeId);
        }

        // 按 nodeId 找目标节点的显示名（speakerName 优先，空则 nodeId）
        private string TargetSpeakerName(string nodeId)
        {
            foreach (Node n in _graphView.nodes)
            {
                if (n is DialogueNodeView nv && nv.Data.nodeId == nodeId)
                {
                    string s = nv.Data.speakerName;
                    return string.IsNullOrEmpty(s) ? nodeId : s;
                }
            }
            return nodeId;
        }

        // ---------------- Conditions 区块（条件挂在 Edge 上） ----------------

        private void BuildConditionsSection(VisualElement parent, List<DialogueConditionData> conditionsList, Action onChanged)
        {
            var label = new Label("Conditions");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8;
            label.style.marginBottom = 2;
            parent.Add(label);

            // 条件列表容器（增删时只重建这一块）
            var listContainer = new VisualElement();

            var addButton = new Button(() =>
            {
                conditionsList.Add(new DialogueConditionData { type = DialogueConditionType.None });
                onChanged?.Invoke();
                RebuildConditions(listContainer, conditionsList, onChanged);
            }) { text = "Add Condition" };
            parent.Add(addButton);

            parent.Add(listContainer);

            RebuildConditions(listContainer, conditionsList, onChanged);
        }

        private void RebuildConditions(VisualElement listContainer, List<DialogueConditionData> conditionsList, Action onChanged)
        {
            listContainer.Clear();

            if (conditionsList == null || conditionsList.Count == 0)
                return;

            for (int i = 0; i < conditionsList.Count; i++)
            {
                int index = i;
                listContainer.Add(BuildConditionCard(conditionsList, conditionsList[index], index, listContainer, onChanged));
            }
        }

        private VisualElement BuildConditionCard(List<DialogueConditionData> conditionsList, DialogueConditionData cond, int index, VisualElement listContainer, Action onChanged)
        {
            var card = new VisualElement();
            var borderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.marginTop = 4;
            card.style.marginBottom = 4;

            var body = new VisualElement();

            // 顶部一行：type + 删除按钮
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;

            var typeField = new EnumField("Type", (Enum)cond.type);
            typeField.RegisterValueChangedCallback(evt =>
            {
                cond.type = (DialogueConditionType)evt.newValue;
                onChanged?.Invoke();
                RebuildConditionCardBody(body, cond);
            });

            var removeBtn = new Button(() =>
            {
                conditionsList.RemoveAt(index);
                onChanged?.Invoke();
                RebuildConditions(listContainer, conditionsList, onChanged);
            }) { text = "X" };
            removeBtn.style.marginLeft = StyleKeyword.Auto;

            topRow.Add(typeField);
            topRow.Add(removeBtn);
            card.Add(topRow);
            card.Add(body);

            RebuildConditionCardBody(body, cond);

            return card;
        }

        private void RebuildConditionCardBody(VisualElement bodyContainer, DialogueConditionData cond)
        {
            bodyContainer.Clear();

            switch (cond.type)
            {
                case DialogueConditionType.None:
                    break;

                case DialogueConditionType.QuestState:
                {
                    var questField = new TextField("questId") { value = cond.questId };
                    questField.RegisterValueChangedCallback(evt => { cond.questId = evt.newValue; });
                    bodyContainer.Add(questField);

                    var stateField = new EnumField("questState", (Enum)cond.questState);
                    stateField.RegisterValueChangedCallback(evt => { cond.questState = (QuestCondition)evt.newValue; });
                    bodyContainer.Add(stateField);
                    break;
                }

                case DialogueConditionType.HasItem:
                {
                    var itemField = new TextField("itemId") { value = cond.itemId };
                    itemField.RegisterValueChangedCallback(evt => { cond.itemId = evt.newValue; });
                    bodyContainer.Add(itemField);

                    var countField = new IntegerField("itemCount") { value = cond.itemCount };
                    countField.RegisterValueChangedCallback(evt => { cond.itemCount = evt.newValue; });
                    bodyContainer.Add(countField);
                    break;
                }

                case DialogueConditionType.HasGold:
                {
                    var countField = new IntegerField("itemCount") { value = cond.itemCount };
                    countField.RegisterValueChangedCallback(evt => { cond.itemCount = evt.newValue; });
                    bodyContainer.Add(countField);
                    break;
                }
            }
        }

        // ---------------- 校验结果数据 ----------------

        private enum IssueSeverity { Error, Warning }

        private class ValidationIssue
        {
            public IssueSeverity severity;
            public string message;
            public DialogueNodeView targetView;
        }
    }
}
