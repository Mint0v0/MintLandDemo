using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 从 CSV 目录读取三个文件（dialogue_node.csv / dialogue_edge.csv / dialogue_condition.csv），
    /// 重建为 DialogueGraph。与 DialogueCsvExporter 互为逆向（幂等）。
    /// </summary>
    public static class DialogueCsvImporter
    {
        /// <summary>
        /// 从 folderPath 读取三个 CSV，返回重建的 DialogueGraph。读取失败返回 null。
        /// </summary>
        public static DialogueGraph Import(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return null;

            string nodePath = Path.Combine(folderPath, "dialogue_node.csv");
            string edgePath = Path.Combine(folderPath, "dialogue_edge.csv");
            string conditionPath = Path.Combine(folderPath, "dialogue_condition.csv");

            if (!File.Exists(nodePath) || !File.Exists(edgePath) || !File.Exists(conditionPath))
            {
                Debug.LogError($"[DialogueCsv] 导入失败：目录 {folderPath} 缺少 dialogue_node.csv / dialogue_edge.csv / dialogue_condition.csv 之一。");
                return null;
            }

            List<List<string>> nodeRows = ParseCsv(File.ReadAllText(nodePath));
            List<List<string>> edgeRows = ParseCsv(File.ReadAllText(edgePath));
            List<List<string>> conditionRows = ParseCsv(File.ReadAllText(conditionPath));

            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.graphId = string.Empty;
            graph.nodes = new List<DialogueNode>();
            graph.edges = new List<DialogueEdge>();

            // 1. 节点（跳过表头）
            for (int i = 1; i < nodeRows.Count; i++)
            {
                List<string> r = nodeRows[i];
                if (r == null || r.Count == 0) continue;

                graph.nodes.Add(new DialogueNode
                {
                    nodeId = Column(r, 0),
                    speakerName = Column(r, 1),
                    dialogueText = Column(r, 2),
                    action = ParseAction(Column(r, 3)),
                    questId = Column(r, 4),
                    itemId = Column(r, 5),
                    itemCount = ParseInt(Column(r, 6), 1),
                    editorPosition = new Vector2(ParseFloat(Column(r, 7), 0f), ParseFloat(Column(r, 8), 0f))
                });
            }

            if (graph.nodes.Count == 0)
            {
                Debug.LogError("[DialogueCsv] 导入失败：dialogue_node.csv 无数据行。");
                UnityEngine.Object.DestroyImmediate(graph);
                return null;
            }

            // 入口节点 = 第一个节点（按 CSV 行顺序）
            graph.entryNodeId = graph.nodes[0].nodeId;

            // 2. 边（先建 edgeId → edge 映射，供条件挂载）
            var edgeById = new Dictionary<string, DialogueEdge>();
            for (int i = 1; i < edgeRows.Count; i++)
            {
                List<string> r = edgeRows[i];
                if (r == null || r.Count == 0) continue;

                var edge = new DialogueEdge
                {
                    edgeId = Column(r, 0),
                    fromNodeId = Column(r, 1),
                    toNodeId = Column(r, 2),
                    choiceText = Column(r, 3),
                    sortOrder = ParseInt(Column(r, 4), 0),
                    action = ParseAction(Column(r, 5)),
                    questId = Column(r, 6),
                    itemId = Column(r, 7),
                    itemCount = ParseInt(Column(r, 8), 1),
                    conditions = new List<DialogueConditionData>()
                };
                graph.edges.Add(edge);
                if (!string.IsNullOrEmpty(edge.edgeId))
                    edgeById[edge.edgeId] = edge;
            }

            // 3. 条件（按 edge_id 挂到对应边）
            for (int i = 1; i < conditionRows.Count; i++)
            {
                List<string> r = conditionRows[i];
                if (r == null || r.Count == 0) continue;

                string edgeId = Column(r, 0);
                if (string.IsNullOrEmpty(edgeId)) continue;

                if (!edgeById.TryGetValue(edgeId, out DialogueEdge edge))
                {
                    Debug.LogWarning($"[DialogueCsv] 条件引用了不存在的 edge_id '{edgeId}'，已忽略。");
                    continue;
                }

                edge.conditions.Add(new DialogueConditionData
                {
                    type = ParseConditionType(Column(r, 2)),
                    questId = Column(r, 3),
                    questState = ParseQuestState(Column(r, 4)),
                    itemId = Column(r, 5),
                    itemCount = ParseInt(Column(r, 6), 1)
                });
            }

            return graph;
        }

        // ---------------- 解析辅助 ----------------

        private static string Column(List<string> row, int index)
        {
            return (index >= 0 && index < row.Count) ? row[index] : string.Empty;
        }

        private static DialogueAction ParseAction(string s)
        {
            if (Enum.TryParse(s, out DialogueAction result)) return result;
            return DialogueAction.None;
        }

        private static DialogueConditionType ParseConditionType(string s)
        {
            if (Enum.TryParse(s, out DialogueConditionType result)) return result;
            return DialogueConditionType.None;
        }

        private static QuestCondition ParseQuestState(string s)
        {
            if (Enum.TryParse(s, out QuestCondition result)) return result;
            return QuestCondition.NotAccepted;
        }

        private static int ParseInt(string s, int defaultValue)
        {
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;
            return defaultValue;
        }

        private static float ParseFloat(string s, float defaultValue)
        {
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// CSV 状态机解析器。不能按行 split（dialogueText 可能含逗号/换行/引号）。
        /// 规则：字段被 " 包裹时内部可含逗号、换行、双引号（"" 表示转义引号）；未包裹时逗号结束字段；
        /// \r\n 或 \n 作行分隔，引号内的换行不算。
        /// </summary>
        private static List<List<string>> ParseCsv(string content)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(content)) return rows;

            var row = new List<string>();
            var field = new System.Text.StringBuilder();
            bool inQuotes = false;
            int i = 0;
            int n = content.Length;

            while (i < n)
            {
                char c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // "" → 转义的双引号
                        if (i + 1 < n && content[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            i++;
                        }
                    }
                    else
                    {
                        field.Append(c);
                        i++;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                        i++;
                    }
                    else if (c == ',')
                    {
                        row.Add(field.ToString());
                        field.Length = 0;
                        i++;
                    }
                    else if (c == '\r')
                    {
                        row.Add(field.ToString());
                        field.Length = 0;
                        rows.Add(row);
                        row = new List<string>();
                        i += (i + 1 < n && content[i + 1] == '\n') ? 2 : 1;
                    }
                    else if (c == '\n')
                    {
                        row.Add(field.ToString());
                        field.Length = 0;
                        rows.Add(row);
                        row = new List<string>();
                        i++;
                    }
                    else
                    {
                        field.Append(c);
                        i++;
                    }
                }
            }

            // 收尾：最后一行没有换行符时补上
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
