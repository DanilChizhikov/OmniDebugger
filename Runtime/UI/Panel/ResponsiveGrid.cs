using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ResponsiveGrid : VisualElement
	{
		public const float TwoColumnsWidth = 460.0f;
		public const float OneColumnWidth = 420.0f;
		public const float WideTwoColumnsWidth = 760.0f;
		public const float WideOneColumnWidth = 720.0f;

		private readonly List<VisualElement> _columnCells;
		private readonly VisualElement _startColumn;
		private readonly VisualElement _endColumn;
		private readonly float _twoColumnsWidth;
		private readonly float _oneColumnWidth;

		private VisualElement _openRow;
		private VisualElement _filler;
		private bool _twoColumns;

		public ResponsiveGrid(bool independentColumns = false, bool wide = false)
		{
			AddToClassList(OmniDebuggerUiClasses.Grid);
			_twoColumnsWidth = wide ? WideTwoColumnsWidth : TwoColumnsWidth;
			_oneColumnWidth = wide ? WideOneColumnWidth : OneColumnWidth;

			if (independentColumns)
			{
				AddToClassList(OmniDebuggerUiClasses.GridColumns);
				_columnCells = new List<VisualElement>();
				_startColumn = Column(OmniDebuggerUiClasses.GridColumnStart);
				_endColumn = Column(OmniDebuggerUiClasses.GridColumnEnd);
				Add(_startColumn);
				Add(_endColumn);
			}

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		public VisualElement AddCell(VisualElement content)
		{
			if (_columnCells != null)
			{
				return AddColumnCell(content);
			}

			bool startsRow = _openRow == null;

			VisualElement cell = Cell(startsRow ? OmniDebuggerUiClasses.GridCellStart : OmniDebuggerUiClasses.GridCellEnd);
			cell.Add(content);

			if (startsRow)
			{
				_openRow = UiBuild.Element(OmniDebuggerUiClasses.GridRow);
				_openRow.Add(cell);
				_filler = Cell(OmniDebuggerUiClasses.GridCellEnd);
				_filler.AddToClassList(OmniDebuggerUiClasses.GridCellFiller);
				_openRow.Add(_filler);
				Add(_openRow);
				return cell;
			}

			_filler.RemoveFromHierarchy();
			_openRow.Add(cell);
			_openRow = null;
			_filler = null;
			return cell;
		}

		private static VisualElement Cell(string sideClassName)
		{
			VisualElement cell = UiBuild.Element(OmniDebuggerUiClasses.GridCell);
			cell.AddToClassList(sideClassName);
			return cell;
		}

		private static VisualElement Column(string sideClassName)
		{
			VisualElement column = UiBuild.Element(OmniDebuggerUiClasses.GridColumn);
			column.AddToClassList(sideClassName);
			return column;
		}

		private VisualElement AddColumnCell(VisualElement content)
		{
			VisualElement cell = UiBuild.Element(OmniDebuggerUiClasses.GridCell);
			cell.Add(content);
			_columnCells.Add(cell);
			ColumnOf(_columnCells.Count - 1).Add(cell);
			return cell;
		}

		private VisualElement ColumnOf(int index) => _twoColumns && index % 2 == 1 ? _endColumn : _startColumn;

		private void DealColumnCells()
		{
			for (int i = 0; i < _columnCells.Count; i++)
			{
				ColumnOf(i).Add(_columnCells[i]);
			}
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			float width = evt.newRect.width;

			if (width <= 0.0f)
			{
				return;
			}

			bool twoColumns = _twoColumns ? width >= _oneColumnWidth : width >= _twoColumnsWidth;

			if (twoColumns == _twoColumns)
			{
				return;
			}

			_twoColumns = twoColumns;
			EnableInClassList(OmniDebuggerUiClasses.GridTwoColumns, twoColumns);

			if (_columnCells != null)
			{
				DealColumnCells();
			}
		}
	}
}