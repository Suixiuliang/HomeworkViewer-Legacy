using System;
using System.Drawing;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public class ResizeHelper
    {
        private Form _form;
        private bool _isResizing;
        private bool _isResizingRow;
        private int _resizeIndex;
        private int _startX, _startY;
        private int _originalWidth, _originalHeight;
        private int[] _rowHeights;
        private int[] _colWidths;
        private Action<int, int> _onResize;

        public ResizeHelper(Form form)
        {
            _form = form;
            _form.MouseDown += OnMouseDown;
            _form.MouseMove += OnMouseMove;
            _form.MouseUp += OnMouseUp;
        }

        public void SetGridMetrics(int[] rowHeights, int[] colWidths, Action<int, int> onResize)
        {
            _rowHeights = rowHeights;
            _colWidths = colWidths;
            _onResize = onResize;
        }

        private void OnMouseDown(object sender, MouseEventArgs e) { }
        private void OnMouseMove(object sender, MouseEventArgs e) { }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                _onResize?.Invoke(_resizeIndex, _colWidths[_resizeIndex] + (e.X - _startX));
                _isResizing = false;
            }
            else if (_isResizingRow)
            {
                _onResize?.Invoke(_resizeIndex, _rowHeights[_resizeIndex] + (e.Y - _startY));
                _isResizingRow = false;
            }
        }
    }
}