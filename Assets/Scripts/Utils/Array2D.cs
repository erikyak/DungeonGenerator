using System;
using UnityEngine;


namespace Utils
{
    [Serializable]
    public class Array2D<T>
    {
        [Min(1)] public int rows = 1;
        [Min(1)] public int cols = 1;
        [SerializeField] private T[] data = new T[1];


        // Auto-expand if change rows/cols in inspector
        public void Resize(int newRows, int newCols)
        {
            var newData = new T[newRows * newCols];
            int minRows = Math.Min(rows, newRows);
            int minCols = Math.Min(cols, newCols);


            for (int r = 0; r < minRows; r++)
            for (int c = 0; c < minCols; c++)
                newData[r * newCols + c] = this[r, c];


            rows = newRows;
            cols = newCols;
            data = newData;
        }


        // indexator
        public T this[int r, int c]
        {
            get => data[r * cols + c];
            set => data[r * cols + c] = value;
        }


        // For external access
        public T[,] To2DArray()
        {
            var arr = new T[rows, cols];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                arr[r, c] = this[r, c];
            return arr;
        }
    }
}