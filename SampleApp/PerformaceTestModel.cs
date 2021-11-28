using System;
using System.Collections.Generic;
using System.Text;
using Aga.Controls.Tree;

namespace SampleApp
{
	public class PerformaceTestModel : ITreeModel
	{
		List<Node> _items = new List<Node>();

		public PerformaceTestModel(int count)
		{
			for (int i = 0; i < count; i++)
				_items.Add(new Node(i.ToString()));
		}

		public System.Collections.IEnumerable GetChildren(TreePath treePath)
		{
			return _items;
		}

		public bool IsLeaf(TreePath treePath)
		{
			return true;
		}
#pragma warning disable CS0067
		public event EventHandler<TreeModelEventArgs> NodesChanged;
		public event EventHandler<TreeModelEventArgs> NodesInserted;
		public event EventHandler<TreeModelEventArgs> NodesRemoved;
		public event EventHandler<TreePathEventArgs> StructureChanged;
#pragma warning restore CS0067
	}
}