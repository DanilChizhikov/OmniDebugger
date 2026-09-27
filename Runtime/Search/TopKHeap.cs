using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class TopKHeap
	{
		private static readonly Comparison<SearchHit> _bestFirst = CompareHits;
		
		public int Count => _count;

		private int[] _ids = Array.Empty<int>();
		private int[] _scores = Array.Empty<int>();
		private byte[] _stages = Array.Empty<byte>();
		private int _capacity;
		private int _count;
		
		public void Reset(int capacity)
		{
			_capacity = Math.Max(1, capacity);
			_count = 0;

			if (_ids.Length >= _capacity)
			{
				return;
			}

			_ids = new int[_capacity];
			_scores = new int[_capacity];
			_stages = new byte[_capacity];
		}
		
		public bool Push(int id, int score, SearchStage stage)
		{
			if (_count < _capacity)
			{
				_ids[_count] = id;
				_scores[_count] = score;
				_stages[_count] = (byte)stage;
				SiftUp(_count);
				_count++;
				return true;
			}

			if (score <= _scores[0])
			{
				return false;
			}

			_ids[0] = id;
			_scores[0] = score;
			_stages[0] = (byte)stage;
			SiftDown(0);
			return true;
		}
		
		public void FillSorted(List<SearchHit> destination)
		{
			destination.Clear();

			for (int i = 0; i < _count; i++)
			{
				destination.Add(new SearchHit(_ids[i], _scores[i], (SearchStage)_stages[i]));
			}

			destination.Sort(_bestFirst);
		}

		private static int CompareHits(SearchHit left, SearchHit right)
		{
			int byScore = right.Score.CompareTo(left.Score);
			return byScore != 0 ? byScore : left.Id.CompareTo(right.Id);
		}

		private void SiftUp(int index)
		{
			while (index > 0)
			{
				int parent = (index - 1) >> 1;
				if (_scores[parent] <= _scores[index])
				{
					return;
				}

				Swap(parent, index);
				index = parent;
			}
		}

		private void SiftDown(int index)
		{
			while (true)
			{
				int left = index * 2 + 1;
				if (left >= _count)
				{
					return;
				}

				int smallest = left;
				int right = left + 1;

				if (right < _count && _scores[right] < _scores[left])
				{
					smallest = right;
				}

				if (_scores[index] <= _scores[smallest])
				{
					return;
				}

				Swap(index, smallest);
				index = smallest;
			}
		}

		private void Swap(int left, int right)
		{
			(_ids[left], _ids[right]) = (_ids[right], _ids[left]);
			(_scores[left], _scores[right]) = (_scores[right], _scores[left]);
			(_stages[left], _stages[right]) = (_stages[right], _stages[left]);
		}
	}
}