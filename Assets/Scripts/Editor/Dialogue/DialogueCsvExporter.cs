using System.Globalization;
using System.IO;
using System.Text;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 把 DialogueGraph 导出为 CSV（UTF-8 with BOM，Excel 打开中文不乱码）。
    /// </summary>
    public static class DialogueCsvExporter
    {
        /// <summary>导出 DialogueGraph 到三个 CSV 文件。</summary>
        public static void Export(DialogueGraph graph, string folderPath)
        {
            if (graph == null || string.IsNullOrEmpty(folderPath)) return;

            WriteFile(Path.Combine(folderPath, "dialogue_node.csv"), BuildNodeCsv(graph));
            WriteFile(Path.Combine(folderPath, "dialogue_edge.csv"), BuildEdgeCsv(graph));
            WriteFile(Path.Combine(folderPath, "dialogue_condition.csv"), BuildConditionCsv(graph));
        }

        private static string BuildNodeCsv(DialogueGraph graph)
        {
            var sb = new StringBuilder();
            sb.AppendLine("node_id,speaker_name,dialogue_text,action,quest_id,item_id,item_count,editor_x,editor_y");

            if (graph.nodes != null)
            {
                foreach (DialogueNode n in graph.nodes)
                {
                    if (n == null) continue;
                    sb.Append(EscapeCsv(n.nodeId)); sb.Append(',');
                    sb.Append(EscapeCsv(n.speakerName)); sb.Append(',');
                    sb.Append(EscapeCsv(n.dialogueText)); sb.Append(',');
                    sb.Append(EscapeCsv(n.action.ToString())); sb.Append(',');
                    sb.Append(EscapeCsv(n.questId)); sb.Append(',');
                    sb.Append(EscapeCsv(n.itemId)); sb.Append(',');
                    sb.Append(n.itemCount.ToString(CultureInfo.InvariantCulture)); sb.Append(',');
                    sb.Append(n.editorPosition.x.ToString(CultureInfo.InvariantCulture)); sb.Append(',');
                    sb.AppendLine(n.editorPosition.y.ToString(CultureInfo.InvariantCulture));
                }
            }

            return sb.ToString();
        }

        private static string BuildEdgeCsv(DialogueGraph graph)
        {
            var sb = new StringBuilder();
            sb.AppendLine("edge_id,from_node_id,to_node_id,choice_text,sort_order,action,quest_id,item_id,item_count");

            if (graph.edges != null)
            {
                foreach (DialogueEdge e in graph.edges)
                {
                    if (e == null) continue;
                    sb.Append(EscapeCsv(e.edgeId)); sb.Append(',');
                    sb.Append(EscapeCsv(e.fromNodeId)); sb.Append(',');
                    sb.Append(EscapeCsv(e.toNodeId)); sb.Append(',');
                    sb.Append(EscapeCsv(e.choiceText)); sb.Append(',');
                    sb.Append(e.sortOrder.ToString(CultureInfo.InvariantCulture)); sb.Append(',');
                    sb.Append(EscapeCsv(e.action.ToString())); sb.Append(',');
                    sb.Append(EscapeCsv(e.questId)); sb.Append(',');
                    sb.Append(EscapeCsv(e.itemId)); sb.Append(',');
                    sb.AppendLine(e.itemCount.ToString(CultureInfo.InvariantCulture));
                }
            }

            return sb.ToString();
        }

        private static string BuildConditionCsv(DialogueGraph graph)
        {
            var sb = new StringBuilder();
            sb.AppendLine("edge_id,condition_index,type,quest_id,quest_state,item_id,item_count");

            if (graph.edges != null)
            {
                foreach (DialogueEdge e in graph.edges)
                {
                    if (e == null || e.conditions == null || e.conditions.Count == 0) continue;

                    for (int i = 0; i < e.conditions.Count; i++)
                    {
                        DialogueConditionData c = e.conditions[i];
                        if (c == null) continue;
                        sb.Append(EscapeCsv(e.edgeId)); sb.Append(',');
                        sb.Append(i.ToString(CultureInfo.InvariantCulture)); sb.Append(',');
                        sb.Append(EscapeCsv(c.type.ToString())); sb.Append(',');
                        sb.Append(EscapeCsv(c.questId)); sb.Append(',');
                        sb.Append(EscapeCsv(c.questState.ToString())); sb.Append(',');
                        sb.Append(EscapeCsv(c.itemId)); sb.Append(',');
                        sb.AppendLine(c.itemCount.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>CSV 字段转义：含逗号 / 引号 / 换行时用双引号包裹，内部引号翻倍。</summary>
        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            bool needsQuotes = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
            if (!needsQuotes)
                return field;

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        private static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(true));
        }
    }
}
