using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Drawing;

namespace Aga.Controls.Tree
{
	internal class ResizeColumnState: ColumnState
	{
		private Point _initLocation;
		private int _initWidth;

		public ResizeColumnState(TreeViewAdv tree, TreeColumn column, Point p)
			: base(tree, column)
		{
			_initLocation = p;
			_initWidth = column.Width;
		}

		public override void KeyDown(KeyEventArgs args)
		{
			args.Handled = true;
			if (args.KeyCode == Keys.Escape)
				FinishResize();
		}

		public override void MouseDown(TreeNodeAdvMouseEventArgs args)
		{
		}

		public override void MouseUp(TreeNodeAdvMouseEventArgs args)
		{
			FinishResize();
		}

		private void FinishResize()
		{
			Tree.ChangeInput();
			Tree.FullUpdate();
			Tree.OnColumnWidthChanged(Column);
		}

        public override bool MouseMove(MouseEventArgs args)
        {
            int w = _initWidth + args.Location.X - _initLocation.X;
            Column.Width = Math.Max(Column.MinColumnWidth, w);

            if (Column.MaxColumnWidth > 0)
            {
                Column.Width = Math.Min(Column.MaxColumnWidth, Column.Width);
            }

            if (Column.Width < 4)
            {
                int i = Column.Index;
                if (i > 0)
                {
                    Column.Owner[i - 1].HiddenColumn = Column;
                }
            }
            else
            {
                int i = Column.Index;
                if (i > 0)
                {
                    Column.Owner[i - 1].HiddenColumn = null;
                }
            }

            Tree.UpdateView();
            return true;
        }
	}
}