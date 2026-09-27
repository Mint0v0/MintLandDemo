using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// DialogueNode 在 GraphView 中的可视化节点。
    /// 标题显示 speakerName；正文显示 nodeId（小字）与 dialogueText 前 30 个字符预览。
    /// </summary>
    public class DialogueNodeView : Node
    {
        public static readonly Vector2 DefaultNodeSize = new Vector2(220, 120);

        // 用节点颜色区分类型：普通节点 / 选择节点（choices 非空）/ 入口节点
        private static readonly Color NormalColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color ChoiceColor = new Color(0.16f, 0.30f, 0.46f, 1f);
        private static readonly Color EntryColor  = new Color(0.20f, 0.42f, 0.24f, 1f);

        public DialogueNode Data { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private readonly VisualElement _contentContainer;
        private bool _isEntry;

        public DialogueNodeView(DialogueNode data)
        {
            Data = data;

            // 输入端口
            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);

            // 输出端口
            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Out";
            outputContainer.Add(OutputPort);

            // 正文内容容器（标题 / 端口不变，内容随 Data 刷新时只重建这里）
            _contentContainer = new VisualElement();
            mainContainer.Add(_contentContainer);

            RefreshFromData();
        }

        /// <summary>根据 Data 重建标题、nodeId 小字与正文预览；字段改动后调用以同步显示。</summary>
        public void RefreshFromData()
        {
            RefreshTitle();
            RebuildContent();
            ApplyTypeColor();
        }

        /// <summary>把该节点标记为入口节点（绿色标题 + 后缀标注）。</summary>
        public void MarkAsEntry()
        {
            _isEntry = true;
            RefreshTitle();
            ApplyTypeColor();
        }

        private void RefreshTitle()
        {
            string speaker = string.IsNullOrEmpty(Data.speakerName) ? "（未命名）" : Data.speakerName;
            title = _isEntry ? speaker + "  [入口]" : speaker;
        }

        private void RebuildContent()
        {
            _contentContainer.Clear();

            // nodeId 小字
            var idLabel = new Label(string.IsNullOrEmpty(Data.nodeId) ? "(无 nodeId)" : Data.nodeId);
            idLabel.style.fontSize = 9;
            idLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            idLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            _contentContainer.Add(idLabel);

            // dialogueText 前 30 个字符
            string text = Data.dialogueText ?? string.Empty;
            if (text.Length > 30) text = text.Substring(0, 30) + "…";
            var textLabel = new Label(text);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            _contentContainer.Add(textLabel);
        }

        private void ApplyTypeColor()
        {
            if (_isEntry)
            {
                titleContainer.style.backgroundColor = EntryColor;
                return;
            }
            bool isChoice = Data.choices != null && Data.choices.Count > 0;
            titleContainer.style.backgroundColor = isChoice ? ChoiceColor : NormalColor;
        }
    }
}
