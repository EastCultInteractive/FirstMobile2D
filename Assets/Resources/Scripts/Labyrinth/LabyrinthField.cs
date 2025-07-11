using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts.Entity.Labyrinth
{
    /// <summary>
    /// Генерирует и управляет полем лабиринта.
    /// </summary>
    public class LabyrinthField
    {
        public LabyrinthCell[,] Field;
        public int Rows { get; }
        public int Cols { get; }

        private float cellSizeX;
        private float cellSizeY;

        private int uniqueCounter = 1;
        private Vector2Int startCell;
        private Vector2Int finishCell;

        public Vector2Int StartCellCoordinates => startCell;
        public Vector2Int FinishCellCoordinates => finishCell;
        public float CellSizeX => cellSizeX;
        public float CellSizeY => cellSizeY;

        public LabyrinthField(int rows, int cols, float cellSizeX, float cellSizeY)
        {
            Rows = rows;
            Cols = cols;
            this.cellSizeX = cellSizeX;
            this.cellSizeY = cellSizeY;
            InitField();
            CreateLabyrinth();
            SetStartAndFinish();
            SolveMaze();
        }

        private void InitField()
        {
            Field = new LabyrinthCell[Rows, Cols];
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Cols; col++)
                    Field[row, col] = new LabyrinthCell();
        }

        private void SetBorders()
        {
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Cols; col++)
                {
                    var cell = Field[row, col];
                    if (row == 0) cell.TopBorder = true;
                    if (col == 0) cell.LeftBorder = true;
                    if (col == Cols - 1) cell.RightBorder = true;
                }
        }

        private bool CanSetBottomBorder(int row, int arrayValue)
        {
            int count = 0, borderCount = 0;
            for (int col = 0; col < Cols; col++)
            {
                var cell = Field[row, col];
                if (cell.ArrayValue == arrayValue)
                {
                    count++;
                    if (cell.BottomBorder) borderCount++;
                }
            }
            return count - 1 != borderCount;
        }

        private void ChangeArrayValues(int row, int oldArrayValue, int newArrayValue)
        {
            for (int col = 0; col < Cols; col++)
                if (Field[row, col].ArrayValue == oldArrayValue)
                    Field[row, col].ArrayValue = newArrayValue;
        }

        private void PreprocessRow(int row)
        {
            if (row == 0) return;

            for (int col = 0; col < Cols; col++)
            {
                Field[row, col].Copy(Field[row - 1, col]);
                Field[row, col].RightBorder = false;
                if (Field[row, col].BottomBorder)
                {
                    Field[row, col].ArrayValue = 0;
                    Field[row, col].BottomBorder = false;
                }
            }
        }

        private void ProcessRow(int row)
        {
            for (int col = 0; col < Cols; col++)
            {
                var cell = Field[row, col];
                if (cell.ArrayValue == 0)
                    cell.ArrayValue = uniqueCounter++;
            }

            for (int col = 0; col < Cols - 1; col++)
            {
                var cell = Field[row, col];
                var nextCell = Field[row, col + 1];
                if (cell.ArrayValue == nextCell.ArrayValue && cell.ArrayValue != 0)
                    cell.RightBorder = true;
                else if (Random.Range(0, 2) == 1)
                    cell.RightBorder = true;
                else
                    nextCell.ArrayValue = cell.ArrayValue;
            }

            for (int col = 0; col < Cols; col++)
            {
                var cell = Field[row, col];
                if (row == Rows - 1)
                {
                    cell.BottomBorder = true;
                    continue;
                }
                if (!CanSetBottomBorder(row, cell.ArrayValue)) continue;
                if (Random.Range(0, 2) == 1)
                    cell.BottomBorder = true;
            }
        }

        private void PostProcess()
        {
            int lastRow = Rows - 1;
            for (int col = 0; col < Cols - 1; col++)
            {
                var cell = Field[lastRow, col];
                var nextCell = Field[lastRow, col + 1];
                if (cell.ArrayValue != nextCell.ArrayValue && cell.RightBorder)
                {
                    cell.RightBorder = false;
                    ChangeArrayValues(lastRow, nextCell.ArrayValue, cell.ArrayValue);
                }
            }
            SetBorders();
        }

        private void CreateLabyrinth()
        {
            for (int row = 0; row < Rows; row++)
            {
                PreprocessRow(row);
                ProcessRow(row);
            }
            PostProcess();
        }

        private void SetStartAndFinish()
        {
            List<Vector2Int> corners = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, Cols - 1),
                new Vector2Int(Rows - 1, 0),
                new Vector2Int(Rows - 1, Cols - 1)
            };

            int startIndex = Random.Range(0, corners.Count);
            startCell = corners[startIndex];
            corners.RemoveAt(startIndex);

            int finishIndex = Random.Range(0, corners.Count);
            finishCell = corners[finishIndex];

            Field[startCell.x, startCell.y].IsStart = true;
            Field[finishCell.x, finishCell.y].IsFinish = true;
        }

        private void SolveMaze()
        {
            List<Vector2Int> shortestPath = FindShortestPath();
            foreach (Vector2Int pos in shortestPath)
                Field[pos.x, pos.y].IsSolutionPath = true;
        }

        private List<Vector2Int> FindShortestPath()
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            bool[,] visited = new bool[Rows, Cols];
            Vector2Int[,] prev = new Vector2Int[Rows, Cols];

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    prev[i, j] = new Vector2Int(-1, -1);

            queue.Enqueue(startCell);
            visited[startCell.x, startCell.y] = true;
            bool found = false;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == finishCell)
                {
                    found = true;
                    break;
                }

                foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
                {
                    if (!visited[neighbor.x, neighbor.y])
                    {
                        visited[neighbor.x, neighbor.y] = true;
                        prev[neighbor.x, neighbor.y] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            List<Vector2Int> path = new List<Vector2Int>();
            if (!found)
                return path;

            Vector2Int curPos = finishCell;
            while (curPos.x != -1 && curPos.y != -1)
            {
                path.Add(curPos);
                curPos = prev[curPos.x, curPos.y];
            }
            path.Reverse();
            return path;
        }

        private List<Vector2Int> GetNeighbors(int row, int col)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            LabyrinthCell cell = Field[row, col];

            if (row > 0 && !cell.TopBorder && !Field[row - 1, col].BottomBorder)
                neighbors.Add(new Vector2Int(row - 1, col));
            if (col < Cols - 1 && !cell.RightBorder && !Field[row, col + 1].LeftBorder)
                neighbors.Add(new Vector2Int(row, col + 1));
            if (row < Rows - 1 && !cell.BottomBorder && !Field[row + 1, col].TopBorder)
                neighbors.Add(new Vector2Int(row + 1, col));
            if (col > 0 && !cell.LeftBorder && !Field[row, col - 1].RightBorder)
                neighbors.Add(new Vector2Int(row, col - 1));

            return neighbors;
        }

        public Vector3 GetFinishWorldPosition()
        {
            return new Vector3(finishCell.y * cellSizeX, -finishCell.x * cellSizeY, 0f);
        }

        public List<Vector3> GetSolutionPathWorldPositions()
        {
            List<Vector2Int> path = FindShortestPath();
            List<Vector3> worldPath = new List<Vector3>();
            foreach (Vector2Int cell in path)
                worldPath.Add(new Vector3(cell.y * cellSizeX, -cell.x * cellSizeY, 0f));
            return worldPath;
        }

        public Vector3 GetStartWorldPosition()
        {
            return new Vector3(startCell.y * cellSizeX, -startCell.x * cellSizeY, 0f);
        }

        /// <summary>
        /// Находит кратчайший путь между двумя произвольными клетками.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int fromCell, Vector2Int toCell)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new bool[Rows, Cols];
            var prev = new Vector2Int[Rows, Cols];

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    prev[i, j] = new Vector2Int(-1, -1);

            queue.Enqueue(fromCell);
            visited[fromCell.x, fromCell.y] = true;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur == toCell) break;

                foreach (var nb in GetNeighbors(cur.x, cur.y))
                {
                    if (!visited[nb.x, nb.y])
                    {
                        visited[nb.x, nb.y] = true;
                        prev[nb.x, nb.y] = cur;
                        queue.Enqueue(nb);
                    }
                }
            }

            var path = new List<Vector2Int>();
            if (!visited[toCell.x, toCell.y])
                return path;

            var at = toCell;
            while (at.x != -1)
            {
                path.Add(at);
                at = prev[at.x, at.y];
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Переводит список клеток в мировые позиции.
        /// </summary>
        public List<Vector3> PathToWorld(List<Vector2Int> cellPath)
        {
            var world = new List<Vector3>();
            foreach (var c in cellPath)
                world.Add(new Vector3(c.y * cellSizeX, -c.x * cellSizeY, 0f));
            return world;
        }
    }
}
