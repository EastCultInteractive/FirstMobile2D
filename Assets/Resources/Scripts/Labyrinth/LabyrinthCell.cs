namespace Resources.Scripts.Entity.Labyrinth
{
    /// <summary>
    /// Представляет отдельную ячейку лабиринта: содержит флаги границ,
    /// числовое значение для генерации и маркеры специальных ячеек.
    /// </summary>
    public class LabyrinthCell
    {
        public bool TopBorder;
        public bool RightBorder;
        public bool BottomBorder;
        public bool LeftBorder;
        public int ArrayValue;
        public bool IsStart;
        public bool IsFinish;
        public bool IsSolutionPath;

        public LabyrinthCell()
        {
            TopBorder = false;
            RightBorder = false;
            BottomBorder = false;
            LeftBorder = false;
            ArrayValue = 0;
            IsStart = false;
            IsFinish = false;
            IsSolutionPath = false;
        }

        public void Copy(LabyrinthCell reference)
        {
            RightBorder = reference.RightBorder;
            BottomBorder = reference.BottomBorder;
            ArrayValue = reference.ArrayValue;
        }
    }
}