using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 对话图的 GraphView：负责节点/边的可视化编辑，以及与 DialogueGraph 数据的同步。
    /// 编辑期间以 _nodeViews / _edges 为唯一数据源，SaveGraph 时统一写回资产。
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        private readonly List<DialogueNodeView> _nodeViews = new List<DialogueNodeView>();
        private readonly List<DialogueEdge> _edges = new List<DialogueEdge>();

        public DialogueGraphView()
        {
            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            graphViewChanged = OnGraphViewChanged;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            foreach (Port p in ports)
            {
                if (p != startPort
                    && p.direction != startPort.direction
                    && p.node != startPort.node)
                {
                    result.Add(p);
                }
            }
            return result;
        }

        /// <summary>清空并重建所有节点和边。</summary>
        public void LoadGraph(DialogueGraph graph)
        {
            _nodeViews.Clear();
            _edges.Clear();

            foreach (GraphElement element in graphElements.ToList())
                RemoveElement(element);

            if (graph == null) return;

            if (graph.nodes == null) graph.nodes = new List<DialogueNode>();
            if (graph.edges == null) graph.edges = new List<DialogueEdge>();

            // 重建节点
            foreach (DialogueNode data in graph.nodes)
            {
                if (data == null) continue;
                if (string.IsNullOrEmpty(data.nodeId))
                    data.nodeId = NewNodeId();

                var view = new DialogueNodeView(data);
                view.SetPosition(new Rect(data.editorPosition, DialogueNodeView.DefaultNodeSize));
                AddElement(view);
                view.RefreshExpandedState();
                view.RefreshPorts();
                _nodeViews.Add(view);
            }

            // 入口节点高亮
            if (!string.IsNullOrEmpty(graph.entryNodeId))
            {
                FindView(graph.entryNodeId)?.MarkAsEntry();
            }

            // 惰性迁移：旧资产的 choices 统一转成 edges（choices 已废弃，仅作兼容保留）
            foreach (DialogueNode data in graph.nodes)
            {
                if (data.choices == null || data.choices.Count == 0) continue;

                bool hasOutgoing = graph.edges.Exists(e => e.fromNodeId == data.nodeId);
                if (hasOutgoing)
                {
                    // 已有出边：choices 是旧数据，直接清空避免重复
                    data.choices.Clear();
                    continue;
                }

                foreach (DialogueChoice c in data.choices)
                {
                    if (c == null) continue;
                    var newEdge = new DialogueEdge
                    {
                        edgeId = "edge_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        fromNodeId = data.nodeId,
                        toNodeId = "",     // 待用户在画布连线补全
                        choiceText = c.choiceText ?? string.Empty,
                        action = c.action,
                        questId = c.questId,
                        itemId = c.itemId,
                        itemCount = c.itemCount,
                        conditions = c.conditions != null
                            ? new List<DialogueConditionData>(c.conditions)
                            : null
                    };
                    graph.edges.Add(newEdge);
                }
                data.choices.Clear();
                Debug.Log($"[DialogueGraph] 迁移 {data.nodeId} 的 choices 到 edges");
            }

            // 迁移后刷新节点显示（choices 已清空，节点类型颜色应随之更新）
            foreach (DialogueNodeView view in _nodeViews)
                view.RefreshFromData();

            // 重建边
            foreach (DialogueEdge edge in graph.edges)
            {
                if (edge == null) continue;
                DialogueNodeView from = FindView(edge.fromNodeId);
                DialogueNodeView to = FindView(edge.toNodeId);

                // 来源节点不存在：丢弃这条坏边
                if (from == null) continue;

                // 目标未连线（迁移产生的空 toNodeId）：保留数据但不画线，待用户在画布补全
                if (to == null)
                {
                    _edges.Add(edge);
                    continue;
                }

                Edge e = from.OutputPort.ConnectTo(to.InputPort);
                e.userData = edge;
                AddElement(e);
                _edges.Add(edge);
            }
        }

        /// <summary>把当前节点位置写回 DialogueNode.editorPosition，并同步 nodes / edges。</summary>
        public void SaveGraph(DialogueGraph graph)
        {
            if (graph == null) return;

            graph.nodes = new List<DialogueNode>();
            foreach (DialogueNodeView view in _nodeViews)
            {
                view.Data.editorPosition = view.GetPosition().position;
                graph.nodes.Add(view.Data);
            }

            graph.edges = new List<DialogueEdge>(_edges);

            // 入口节点兜底：未设置时取第一个节点
            if (string.IsNullOrEmpty(graph.entryNodeId) && graph.nodes.Count > 0)
                graph.entryNodeId = graph.nodes[0].nodeId;
        }

        /// <summary>创建单个节点视图（及其数据）。</summary>
        public void CreateNode(DialogueNode data, Vector2 position)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.nodeId))
                data.nodeId = NewNodeId();

            var view = new DialogueNodeView(data);
            view.SetPosition(new Rect(position, DialogueNodeView.DefaultNodeSize));
            AddElement(view);
            view.RefreshExpandedState();
            view.RefreshPorts();
            _nodeViews.Add(view);

            // 新建后自动选中，方便右侧面板立即编辑
            ClearSelection();
            AddToSelection(view);
        }

        // ---------------- 边访问接口（供 Inspector 使用） ----------------

        /// <summary>返回所有边的数据副本。</summary>
        public List<DialogueEdge> GetAllEdges()
        {
            return new List<DialogueEdge>(_edges);
        }

        /// <summary>返回从指定节点出发的所有边。</summary>
        public List<DialogueEdge> GetEdgesForNode(string nodeId)
        {
            return _edges.Where(e => e.fromNodeId == nodeId).ToList();
        }

        /// <summary>删除一条边：同步移除数据与画布上的视觉边。</summary>
        public void RemoveEdge(DialogueEdge edge)
        {
            if (edge == null) return;

            _edges.Remove(edge);

            foreach (Edge e in edges.ToList())
            {
                if (e.userData == edge)
                    RemoveElement(e);
            }
        }

        /// <summary>通知画布某条边的数据已改变，刷新其视觉表现。</summary>
        public void NotifyEdgeChanged(DialogueEdge edge)
        {
            if (edge == null) return;
            foreach (Edge e in edges)
            {
                if (e.userData == edge)
                {
                    e.MarkDirtyRepaint();
                    break;
                }
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePos = evt.localMousePosition;

            // 向上遍历找到最近的 GraphElement，判断点击目标（空白 / 节点 / 边 / 端口）
            VisualElement target = evt.target as VisualElement;
            GraphElement hitElement = null;
            while (target != null && target != this)
            {
                if (target is GraphElement ge)
                {
                    hitElement = ge;
                    break;
                }
                target = target.parent;
            }

            if (hitElement == null)
            {
                // 空白处：只加建节点
                AppendAddNodeAction(evt, mousePos);
            }
            else
            {
                // 节点/边/端口上：先让基类加默认菜单项（Delete / Duplicate / Disconnect 等）
                base.BuildContextualMenu(evt);

                if (hitElement is DialogueNodeView nodeView)
                {
                    evt.menu.AppendSeparator();

                    evt.menu.AppendAction("Delete", _ =>
                    {
                        ClearSelection();
                        AddToSelection(nodeView);
                        DeleteSelection();
                    });

                    evt.menu.AppendAction("Duplicate", _ =>
                    {
                        var src = nodeView.Data;
                        var copy = new DialogueNode
                        {
                            nodeId = NewNodeId(),
                            speakerName = src.speakerName,
                            dialogueText = src.dialogueText,
                            action = src.action,
                            questId = src.questId,
                            itemId = src.itemId,
                            itemCount = src.itemCount,
                            choices = CopyChoices(src.choices)
                        };
                        Vector2 srcPos = nodeView.GetPosition().position;
                        CreateNode(copy, srcPos + new Vector2(30, 30));
                    });

                    evt.menu.AppendSeparator();
                    AppendAddNodeAction(evt, mousePos);
                }
            }
        }

        private void AppendAddNodeAction(ContextualMenuPopulateEvent evt, Vector2 mousePos)
        {
            evt.menu.AppendAction("Add Dialogue Node", _ =>
            {
                var data = new DialogueNode
                {
                    nodeId = NewNodeId(),
                    speakerName = "新节点"
                };
                CreateNode(data, ToGraphPosition(mousePos));
            });
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // 边创建 → 同步 DialogueEdge 数据
            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    DialogueNodeView from = edge.output?.node as DialogueNodeView;
                    DialogueNodeView to = edge.input?.node as DialogueNodeView;
                    if (from == null || to == null) continue;

                    var data = new DialogueEdge
                    {
                        edgeId = "edge_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        fromNodeId = from.Data.nodeId,
                        toNodeId = to.Data.nodeId
                    };
                    edge.userData = data;
                    _edges.Add(data);
                }
            }

            // 删除 → 同步节点 / 边数据
            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove.ToList())
                {
                    if (element is DialogueNodeView nodeView)
                    {
                        _nodeViews.Remove(nodeView);
                        _edges.RemoveAll(e => e.fromNodeId == nodeView.Data.nodeId || e.toNodeId == nodeView.Data.nodeId);

                        // 一并删除与之相连的视觉边
                        foreach (Edge e in edges.ToList())
                        {
                            DialogueNodeView outNode = e.output?.node as DialogueNodeView;
                            DialogueNodeView inNode = e.input?.node as DialogueNodeView;
                            if (outNode == nodeView || inNode == nodeView)
                            {
                                if (!change.elementsToRemove.Contains(e))
                                    change.elementsToRemove.Add(e);
                            }
                        }
                    }
                    else if (element is Edge edge)
                    {
                        DialogueEdge de = edge.userData as DialogueEdge;
                        if (de != null) _edges.Remove(de);
                    }
                }
            }

            return change;
        }

        private DialogueNodeView FindView(string nodeId)
        {
            foreach (DialogueNodeView view in _nodeViews)
                if (view.Data.nodeId == nodeId) return view;
            return null;
        }

        private static string NewNodeId()
        {
            return "node_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static List<DialogueChoice> CopyChoices(List<DialogueChoice> src)
        {
            if (src == null) return null;
            var list = new List<DialogueChoice>();
            foreach (var c in src)
            {
                if (c == null) { list.Add(null); continue; }
                list.Add(new DialogueChoice
                {
                    choiceText = c.choiceText,
                    nextNodeId = c.nextNodeId,
                    action = c.action,
                    questId = c.questId,
                    itemId = c.itemId,
                    itemCount = c.itemCount,
                    conditions = c.conditions != null
                        ? new List<DialogueConditionData>(c.conditions)
                        : null
                });
            }
            return list;
        }

        private Vector2 ToGraphPosition(Vector2 localPosition)
        {
            Vector2 worldPos = this.LocalToWorld(localPosition);
            return contentViewContainer.WorldToLocal(worldPos);
        }
    }
}