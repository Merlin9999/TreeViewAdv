using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using Aga.Controls.Tree.NodeControls;

namespace Aga.Controls.Tree
{
	public partial class TreeViewAdv
	{

		private void CreatePens()
		{
			CreateLinePen();
			CreateMarkPen();
		}

		private void CreateMarkPen()
		{
			GraphicsPath path = new GraphicsPath();
			path.AddLines(new Point[] { new Point(0, 0), new Point(1, 1), new Point(-1, 1), new Point(0, 0) });
			CustomLineCap cap = new CustomLineCap(null, path);
			cap.WidthScale = 1.0f;

			_markPen = new Pen(_dragDropMarkColor, _dragDropMarkWidth);
			_markPen.CustomStartCap = cap;
			_markPen.CustomEndCap = cap;
		}

		private void CreateLinePen()
		{
			_linePen = new Pen(_lineColor);
			_linePen.DashStyle = DashStyle.Dot;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			BeginPerformanceCount();

			DrawContext context = new DrawContext();
			context.Graphics = e.Graphics;
			context.Font = this.Font;
			context.Enabled = Enabled;

			int y = 0;
			if (UseColumns)
			{
				DrawColumnHeaders(e.Graphics);
				y += ColumnHeaderHeight;
				if (Columns.Count == 0)
					return;
			}
			y -= _rowLayout.GetRowBounds(FirstVisibleRow).Y;

			e.Graphics.ResetTransform();
			e.Graphics.TranslateTransform(-OffsetX, y);
			Rectangle displayRect = DisplayRectangle;
			for (int row = FirstVisibleRow; row < RowCount; row++)
			{
				Rectangle rowRect = _rowLayout.GetRowBounds(row);
				if (rowRect.Y + y > displayRect.Bottom)
					break;
				else
					DrawRow(e, ref context, row, rowRect);
			}

			if (Search.IsActive)
				Search.Draw(context);

			if (_dropPosition.Node != null && DragMode)
				DrawDropMark(e.Graphics);

			e.Graphics.ResetTransform();
			DrawScrollBarsBox(e.Graphics);

			if (DragMode && _dragBitmap != null)
				e.Graphics.DrawImage(_dragBitmap, PointToClient(MousePosition));

			EndPerformanceCount(e);
		}

		private void DrawRow(PaintEventArgs e, ref DrawContext context, int row, Rectangle rowRect)
		{
			TreeNodeAdv node = _rowMap[row];
			context.DrawSelection = DrawSelectionMode.None;
			context.CurrentEditorOwner = _currentEditorOwner;
			if (DragMode)
			{
				if ((_dropPosition.Node == node) && _dropPosition.Position == NodePosition.Inside)
					context.DrawSelection = DrawSelectionMode.Active;
			}
			else
			{
				if (node.IsSelected && Focused)
					context.DrawSelection = DrawSelectionMode.Active;
				else if (node.IsSelected && !Focused && !HideSelection)
					context.DrawSelection = DrawSelectionMode.Inactive;
			}
			context.DrawFocus = Focused && CurrentNode == node;

			if (FullRowSelect)
			{
				context.DrawFocus = false;
				if (context.DrawSelection == DrawSelectionMode.Active || context.DrawSelection == DrawSelectionMode.Inactive)
				{
					Rectangle focusRect = new Rectangle(OffsetX, rowRect.Y, ClientRectangle.Width, rowRect.Height);
					if (context.DrawSelection == DrawSelectionMode.Active)
					{
						e.Graphics.FillRectangle(SystemBrushes.Highlight, focusRect);
						context.DrawSelection = DrawSelectionMode.FullRowSelect;
					}
					else
					{
						e.Graphics.FillRectangle(SystemBrushes.InactiveBorder, focusRect);
						context.DrawSelection = DrawSelectionMode.None;
					}
				}
			}

			if (ShowLines)
				DrawLines(e.Graphics, node, rowRect);

			DrawNode(node, context);
		}

		private void DrawColumnHeaders(Graphics gr)
		{
			int x = 0;
			VisualStyleRenderer renderer = null;
			if (Application.RenderWithVisualStyles)
				renderer = new VisualStyleRenderer(VisualStyleElement.Header.Item.Normal);

			DrawHeaderBackground(gr, renderer, new Rectangle(0, 0, ClientRectangle.Width + 10, ColumnHeaderHeight));
			gr.TranslateTransform(-OffsetX, 0);
			foreach (TreeColumn c in Columns)
			{
				if (c.IsVisible)
				{
					Rectangle rect = new Rectangle(x, 0, c.Width, ColumnHeaderHeight);
					x += c.Width;
					DrawHeaderBackground(gr, renderer, rect);
					c.Draw(gr, rect, Font);
				}
			}
		}

		private static void DrawHeaderBackground(Graphics gr, VisualStyleRenderer renderer, Rectangle rect)
		{
			if (renderer != null)
				renderer.DrawBackground(gr, rect);
			else
			{
				gr.FillRectangle(SystemBrushes.Control, rect);

				gr.DrawLine(SystemPens.ControlDark, rect.X, rect.Bottom - 2, rect.Right, rect.Bottom - 2);
				gr.DrawLine(SystemPens.ControlLightLight, rect.X, rect.Bottom - 1, rect.Right, rect.Bottom - 1);

				gr.DrawLine(SystemPens.ControlDark, rect.Right - 2, rect.Y, rect.Right - 2, rect.Bottom - 2);
				gr.DrawLine(SystemPens.ControlLightLight, rect.Right - 1, rect.Y, rect.Right - 1, rect.Bottom - 1);
			}
		}

		public void DrawNode(TreeNodeAdv node, DrawContext context)
		{
			foreach (NodeControlInfo item in GetNodeControls(node))
			{
				context.Bounds = item.Bounds;
				context.Graphics.SetClip(context.Bounds);
				item.Control.Draw(node, context);
				context.Graphics.ResetClip();
			}
		}

		private void DrawScrollBarsBox(Graphics gr)
		{
			Rectangle r1 = DisplayRectangle;
			Rectangle r2 = ClientRectangle;
			gr.FillRectangle(SystemBrushes.Control,
				new Rectangle(r1.Right, r1.Bottom, r2.Width - r1.Width, r2.Height - r1.Height));
		}

		private void DrawDropMark(Graphics gr)
		{
			if (_dropPosition.Position == NodePosition.Inside)
				return;

			Rectangle rect = GetNodeBounds(_dropPosition.Node);
			int right = DisplayRectangle.Right - LeftMargin + OffsetX;
			int y = rect.Y;
			if (_dropPosition.Position == NodePosition.After)
				y = rect.Bottom;
			gr.DrawLine(_markPen, rect.X, y, right, y);
		}

		private void DrawLines(Graphics gr, TreeNodeAdv node, Rectangle rowRect)
		{
			if (UseColumns && Columns.Count > 0)
				gr.SetClip(new Rectangle(0, rowRect.Y, Columns[0].Width, rowRect.Bottom));

			TreeNodeAdv curNode = node;
			while (curNode != _root && curNode != null)
			{
				int level = curNode.Level;
				int x = (level - 1) * _indent + NodePlusMinus.ImageSize / 2 + LeftMargin;
				int width = NodePlusMinus.Width - NodePlusMinus.ImageSize / 2;
				int y = rowRect.Y;
				int y2 = y + rowRect.Height;

				if (curNode == node)
				{
					int midy = y + rowRect.Height / 2;
					gr.DrawLine(_linePen, x, midy, x + width, midy);
					if (curNode.NextNode == null)
						y2 = y + rowRect.Height / 2;
				}

				if (node.Row == 0)
					y = rowRect.Height / 2;
				if (curNode.NextNode != null || curNode == node)
					gr.DrawLine(_linePen, x, y, x, y2);

				curNode = curNode.Parent;
			}

			gr.ResetClip();
		}

		#region Performance

		private float _totalTime;
		private int _paintCount;

		[Conditional("PERF_TEST")]
		private void BeginPerformanceCount()
		{
			_paintCount++;
			TimeCounter.Start();
		}

		[Conditional("PERF_TEST")]
		private void EndPerformanceCount(PaintEventArgs e)
		{
			float time = TimeCounter.Finish();
			_totalTime += time;
			string debugText = string.Format("FPS {0:0.0}; Avg. FPS {1:0.0}",
				1 / time, 1 / (_totalTime / _paintCount));
			e.Graphics.DrawString(debugText, Control.DefaultFont, Brushes.Gray,
				new PointF(DisplayRectangle.Width - 150, DisplayRectangle.Height - 20));
		}
		#endregion

	}
}