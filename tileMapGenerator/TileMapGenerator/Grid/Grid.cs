using System.Numerics;
using System.Collections;
using System.Drawing;
using TileMapGenerator;
using Primitives;
using System.Collections.Concurrent;
using System.Diagnostics;
using SadRogue.Primitives.GridViews;
using Vector2Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using static Medallion.Bits;

namespace Grid
{
    public struct Grid : IDed, IGridView<bool>
    {
        internal Backing _grid;
        private (uint, uint) _size;
        public uint RowSize{get{return _size.Item1;}}
        public uint ColSize{get{return _size.Item2;}}

        public int ID { get => _id; set => _id = value; }

        public int Height => (int) RowSize;

        public int Width => (int) ColSize;

        public int Count => _grid.Count;

        public bool this[int index1D] {
            get
            {
                return GetCell(GetPositionFromIndex((uint) index1D)) == 1;
            }
            set
            {
                SetCell(GetPositionFromIndex((uint) index1D), value ? 1u : 0);
            }
        }

        public bool this[SadRogue.Primitives.Point pos]  {
            get
            {
                return GetCell(pos.ToVector2()) == 1;
            }
            set
            {
                SetCell(pos.ToVector2(), value ? 1u : 0u);
            }
        }

        public bool this[int x, int y]  {
            get
            {
                return GetCell((uint) x, (uint) y) == 1u;
            }
            set
            {
                SetCell((uint) x, (uint) y, value ? 1u : 0u);
            }
        }

        private uint _border_num;
        private int _id;
        private bool[] _queue_fill;
        private bool[] _queue_empty;
        public Grid(uint rows, uint columns, uint borders = 1)
        {
            _border_num = 1;
            _id = GetID(rows, columns);
            if (rows == 0 || columns == 0)
            {
                throw new IndexOutOfRangeException();
            }
            _grid = new Backing((int)((rows + 2) * (columns + 2)), 0);
            _queue_fill = new bool[(rows + 2) * (columns + 2)];
            _queue_empty = new bool[(rows + 2) * (columns + 2)];

            _size = (rows, columns);
            if (borders == 1)
            {
                ChangeBorders(borders);
            }
            else if (borders != 0)
            {
                throw new ArgumentException("Value must be 0 or 1. Value was: " + borders);
            }
        }

        public Grid(Grid old_grid)
        {
            _border_num = 1;
            _id = GetID(old_grid._size.Item1, old_grid._size.Item2, old_grid._grid.ToString());
            if (old_grid._size.Item1 == 0 || old_grid._size.Item2 == 0)
            {
                throw new IndexOutOfRangeException();
            }
            _grid = new Backing((int)((old_grid._size.Item1 + 2) * (old_grid._size.Item2 + 2)), 0);
            _queue_fill = new bool[(old_grid._size.Item1 + 2) * (old_grid._size.Item2 + 2)];
            _queue_empty = new bool[(old_grid._size.Item1 + 2) * (old_grid._size.Item2 + 2)];

            _size = (old_grid._size.Item1, old_grid._size.Item2);
            if (old_grid._border_num == 1)
            {
                ChangeBorders(old_grid._border_num);
            }
            else if (old_grid._border_num != 0)
            {
                throw new ArgumentException("Value must be 0 or 1. Value was: " + old_grid._border_num);
            }
            _grid = new Backing(old_grid._grid);
        }

        public void Clear()
        {
            InnerConstructor(_size.Item1, _size.Item2, _border_num);
        }

        private static  int GetID(uint rows, uint columns, string grid = "")
        {
            return UIDGenerator.GetNextID(rows * columns + grid);
        }

        private void InnerConstructor(uint rows, uint columns, uint borders)
        {
            if (rows == 0 || columns == 0)
            {
                throw new IndexOutOfRangeException();
            }
            _grid = new Backing((int)((rows + 2) * (columns + 2)), 0);
            _queue_fill = new bool[(rows + 2) * (columns + 2)];
            _queue_empty = new bool[(rows + 2) * (columns + 2)];

            _size = (rows, columns);
            if (borders == 1)
            {
                ChangeBorders(borders);
            }
            else if (borders != 0)
            {
                throw new ArgumentException("Value must be 0 or 1. Value was: " + borders);
            }
        }

        public void ChangeBorders(uint borders)
        {
            if (borders != 1 && borders != 0)
            {
                throw new ArgumentException("Value must be 0 or 1, not: " + borders);
            }
            SetRowInternal(0, _size.Item2+1, borders, _size.Item2+1 + 1);
            SetRowInternal(_size.Item1+1, _size.Item2+1, borders, _size.Item2+1 + 1);
            for (uint row = 0; row < _size.Item1+2; row++)
            {
                SetCellInternal(row, 0, borders);
                SetCellInternal(row, _size.Item2+1, borders);
            }
            _border_num = borders;
        }

        private uint GetCellIndex(uint row, uint col){
            return row*(_size.Item2+2) + col;
        }

        private Vector2 GetPositionFromIndex(uint index)
        {
            return new Vector2(index / _size.Item2, index % _size.Item2);
        }

        public void SetCell(uint row, uint col, uint val)
        {
            if (val != 0 && val != 1)
            {
                throw new ArgumentException("Value must be 0 or 1, not: " + val);
            } 
            else if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                throw new IndexOutOfRangeException("Bad Cell Index: (" + row + ", " + col + ")");
            }

            SetCellInternal(row, col, val);
        }

        public void SetCell(Vector2 coords, uint val)
        {
            SetCell((uint) coords.X, (uint) coords.Y, val);
        }

        public void QueueFillCell(uint row, uint col)
        {
            if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                throw new IndexOutOfRangeException("Bad Cell Index: (" + row + ", " + col + ")");
            }
            _queue_fill[GetCellIndex(row, col)] = true;
        }

        public void QueueEmptyCell(uint row, uint col)
        {
            if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                throw new IndexOutOfRangeException("Bad Cell Index: (" + row + ", " + col + ")");
            }
            _queue_empty[GetCellIndex(row, col)] = true;
        }

        public void RunQueue()
        {
            _grid |= new Backing(new BitArray(_queue_fill));
            _queue_fill = new bool[_queue_fill.Length];
            _grid &= (~new Backing(new BitArray(_queue_empty))) &_grid;
            _queue_empty = new bool[_queue_empty.Length];
        }

        public void CombineGrids(IEnumerable<Grid> other_grids)
        {
            foreach (var o_grid in other_grids)
            {
                _grid |= o_grid._grid;
            }
        }

        public void DifferenceGrids(IEnumerable<Grid> other_grids)
        {
            _grid |= other_grids.Select(g=>g._grid).Aggregate((g1, g2)=>g1 | g2);
            _grid &= (~other_grids.Select(g=>g._grid).Aggregate((g1, g2)=>g1 | g2)) &_grid;
        }

        private void SetCellInternal(uint row, uint col, uint val)
        {
            if (val == 1){
                _grid.SetBit((int) GetCellIndex(row, col), true);
            }
            else
            {
                _grid.SetBit((int) GetCellIndex(row, col), false);
            }
        }

        private void SetRowInternal(uint row, uint col, uint val, uint length)
        {        
            if (val == 1){
                _grid |= new Backing((int) length, 1) <<  (int) (GetCellIndex(row, col)+1-length);
            }
            else
            {
                _grid &= ~((new Backing((int) length, 1) <<  (int) (GetCellIndex(row, col)+1-length))&_grid);
            }
        }

        public uint GetCell(uint row, uint col)
        {
            if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                throw new IndexOutOfRangeException("Bad Cell Index: (" + row + ", " + col + ")");
            }

            return GetCellInternal(row, col);
        }

        public uint GetCell(Vector2 coords)
        {
            return GetCell((uint) coords.X, (uint) coords.Y);
        }

        private uint GetCellInternal(uint row, uint col)
        {
            return _grid.GetBit((int) GetCellIndex(row, col));
        }

        private void InsertEmptyCellInternal(uint row, uint col)
        {
            int cell_index = (int) GetCellIndex(row, col);
            Backing first_half = _grid & new Backing(cell_index, 1);
            Backing second_half = (_grid << 1) & (new Backing(_grid.Count - cell_index, 1) << (cell_index+1));
            _grid = first_half | second_half;
        }

        private void InsertEmptyRowInternal(uint row, uint col, uint length)
        {
            int cell_index = (int) GetCellIndex(row, col);
            Backing first_half = _grid & new Backing(cell_index, 1);
            Backing second_half = (_grid << (int) length+1) & (new Backing(_grid.Count - cell_index, 1) << (int) (cell_index+length+1));
            _grid = first_half | second_half;
        }

        private void DeleteCellInternal(uint row, uint col)
        {
            int cell_index = (int) GetCellIndex(row, col);
            Backing first_half = _grid & new Backing(cell_index, 1);
            Backing second_half = (_grid >> 1) & (new Backing(_grid.Count - cell_index, 1) << cell_index);
            _grid = first_half | second_half;
        }

        private void DeleteEmptyRowInternal(uint row, uint col, uint length)
        {
            int cell_index = (int) GetCellIndex(row, col);
            Backing first_half = _grid & new Backing(cell_index, 1);
            Backing second_half = (_grid >> (int) length+1) & (new Backing(_grid.Count - cell_index, 1) << (int) (cell_index-length+1));
            _grid = first_half | second_half;
        }

        public uint GetCellNeighbors(uint row, uint col)
        {
            uint neighbors = 0;
            neighbors = neighbors << 1 | GetCellInternal(row, col);
            neighbors = neighbors << 1 | GetCellInternal(row, col+1);
            neighbors = neighbors << 1 | GetCellInternal(row-1, col+1);
            neighbors = neighbors << 1 | GetCellInternal(row-1, col);
            neighbors = neighbors << 1 | GetCellInternal(row-1, col-1);
            neighbors = neighbors << 1 | GetCellInternal(row, col-1);
            neighbors = neighbors << 1 | GetCellInternal(row+1, col-1);
            neighbors = neighbors << 1 | GetCellInternal(row+1, col);
            neighbors = neighbors << 1 | GetCellInternal(row+1, col+1);
            return neighbors;
        }

        public uint GetAllSetCellNeighbors(uint row, uint col)
        {
            return (uint) BitCount(GetCellNeighbors(row, col));
        }

        public uint GetAllSetCartesianNeighbors(uint row, uint col)
        {
            uint neighbors = 0;
            neighbors += GetCellInternal(row, col);
            neighbors += GetCellInternal(row, col+1);
            neighbors += GetCellInternal(row-1, col);
            neighbors += GetCellInternal(row, col-1);
            neighbors += GetCellInternal(row+1, col);
            return neighbors;
        }

        private void CheckIndexValidity(uint row, uint col)
        {
            if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                throw new IndexOutOfRangeException("Bad Cell Index: (" + row + ", " + col + ")");
            }
        }

        private void ValidateNewSize(int size_change_x, int size_change_y)
        {
            if (_size.Item1 + size_change_x <= 0 || _size.Item2 + size_change_y <= 0)
            {
                throw new IndexOutOfRangeException("Graph cannot be smaller than 1 row or column.");
            }
        }

        public void InsertRow(uint index)
        {
            CheckIndexValidity(index, 1);
            InsertEmptyRowInternal(index, 0, _size.Item2+1);
            _size.Item1++;
            ChangeBorders(_border_num);
        }

        public void InsertCol(uint index)
        {
            CheckIndexValidity(1, index);
            for (int row = (int) _size.Item1 + 1; row >= 0; row--)
            {
                InsertEmptyCellInternal((uint) row, index);
            }
            _size.Item2++;
            ChangeBorders(_border_num);
        }

        public void DeleteRow(uint index)
        {
            CheckIndexValidity(index, 1);
            ValidateNewSize(-1, 0);
            DeleteEmptyRowInternal(index, 0,  _size.Item2+1);
            _size.Item1--;
        }

        public void DeleteCol(uint index)
        {
            CheckIndexValidity(1, index);
            ValidateNewSize(0, -1);
            for (int row = (int) _size.Item1+1 ; row >= 0; row--)
            {
                DeleteCellInternal((uint) row, index);
            }
            _size.Item2--;
        }

        public void SetSlice(uint row1, uint col1, uint row2, uint col2, uint val)
        {
            if (val != 0 && val != 1)
            {
                throw new ArgumentException("Value must be 0 or 1, not: " + val);
            } 
            CheckIndexValidity(row1, col1);
            CheckIndexValidity(row2, col2);

            if (row1 == row2)
            {
                uint min_col = Math.Min(col1, col2);
                uint max_col = Math.Max(col1, col2);

                SetRowInternal(row1, max_col, val, max_col - min_col + 1);

            }
            else if (col1 == col2)
            {
                uint min_row = Math.Min(row1, row2);
                uint max_row = Math.Max(row1, row2);

                for (uint row = min_row; row <= max_row; row++)
                {
                    SetCell(row, col1, val);
                } 
            }
            else
            {
                throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
            }
        }

        public ulong GetSlice(uint row1, uint col1, uint row2, uint col2)
        {
            ulong result = 0;
            if (row1 == row2)
            {
                uint min_col = Math.Min(col1, col2);
                uint max_col = Math.Max(col1, col2);

                for (uint col = min_col; col <= max_col; col++)
                {
                    if (col == min_col)
                    {
                        result = GetCell(row1, min_col);
                    }
                    else
                    {
                        result = (result << 1) | GetCell(row1, col);
                    }
                }
            }
            else if (col1 == col2)
            {
                uint min_row = Math.Min(row1, row2);
                uint max_row = Math.Max(row1, row2);

                for (uint row = min_row; row <= max_row; row++)
                {
                    if (row == min_row)
                    {
                        result = GetCell( row, col1);
                    }
                    else
                    {
                        result = (result << 1) | GetCell(row, col1);
                    }
                } 
            }
            else
            {
                throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
            }
            return result;
        }

        public uint GetSliceOR(uint row1, uint col1, uint row2, uint col2)
        {
            CheckIndexValidity(row1, col1);
            CheckIndexValidity(row2, col2);
            uint result = 0;
            if (row1 == row2)
            {
                uint min_col = Math.Min(col1, col2);
                uint max_col = Math.Max(col1, col2);

                for (uint col = min_col; col <= max_col; col++)
                {
                    result |= GetCell(row1, col);
                    if (result > 0)
                    {
                        break;
                    }
                }
            }
            else if (col1 == col2)
            {
                uint min_row = Math.Min(row1, row2);
                uint max_row = Math.Max(row1, row2);

                for (uint row =  min_row; row <= max_row; row++)
                {
                    result |=  GetCell(row, col1);
                    if (result > 0)
                    {
                        break;
                    }
                } 
            }
            else
            {
                throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
            }
            return result;
        }

        public uint GetSliceAND(uint row1, uint col1, uint row2, uint col2)
        {
            CheckIndexValidity(row1, col1);
            CheckIndexValidity(row2, col2);
            uint result = 1;
            if (row1 == row2)
            {
                uint min_col = Math.Min(col1, col2);
                uint max_col = Math.Max(col1, col2);

                for (uint col = min_col; col <= max_col; col++)
                {
                    result &= GetCell(row1, col);
                    if (result == 0)
                    {
                        break;
                    }
                }
            }
            else if (col1 == col2)
            {
                uint min_row = Math.Min(row1, row2);
                uint max_row = Math.Max(row1, row2);

                for (uint row =  min_row; row <= max_row; row++)
                {
                    result &=  GetCell(row, col1);
                    if (result == 0)
                    {
                        break;
                    }
                } 
            }
            else
            {
                throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
            }
            return result;
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is Grid)
            {
                return ((Grid) obj)._grid.Equals(_grid);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public bool InBounds(uint row, uint col)
        {
            if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
            {
                return false;
            }
            return true;
        }

        public bool InBounds(Vector2 coords)
        {
            if (coords.X < 0 || coords.Y < 0)
            {
                return false;
            }
            return InBounds((uint) coords.X, (uint) coords.Y);
        }

        #if DEBUG
        public void ToBMP(string name = "", string color = "0xFF000000", bool overlay = false)
        {
            Image image;
            Graphics graphic;
            if (!overlay)
            {
                image = new Bitmap((int) (30*_size.Item2), (int) (30*_size.Item1));
                graphic = Graphics.FromImage(image);
            }
            else
            {
                File.Copy($"../../{name}Grid.bmp", $"../../__temp__{name}Grid.bmp");
                image = Image.FromFile($"../../__temp__{name}Grid.bmp");
                graphic = Graphics.FromImage(image);
            }
        
            using var brush = new SolidBrush(Color.FromArgb(Convert.ToInt32(color, 16)));

            if (!overlay)
            {
                graphic.Clear(Color.White);
            }
        
            for(int row = 1; row <= _size.Item1; row++)
            {
                for(int col = 1; col <= _size.Item2; col++)
                {
                    if (GetCell((uint) row, (uint) col) == 1)
                    {
                        graphic.FillRectangle(brush, new RectangleF(30*(col-1), 30*(row-1), 30f, 30f));
                    }
                }
            }
            if (overlay)
            {
                File.Delete($"../../{name}Grid.bmp");
            }
            image.Save($"../../{name}Grid.bmp");
            graphic.Dispose();
            image.Dispose();
            if (overlay)
            {
                File.Delete($"../../__temp__{name}Grid.bmp");
            }     
        }
        #endif
    }
    

    internal struct Backing
        {
            private BitArray _backing_array;
            public Backing(ulong value)
            {
                _backing_array = ToBitArray(value);
            } 

            public Backing(Backing old_number)
            {
                _backing_array = new BitArray(old_number._backing_array);
            }

            public static Backing ZERO{get;} = new Backing(0);
            public static Backing ONE{get;} = new Backing(1);

            public int Count{get {return _backing_array.Count;}}

            public Backing(int bit_number, int default_value)
            {
                if (default_value != 1 && default_value != 0)
                {
                    throw new ArgumentException("number must be 0 or 1");
                }
                _backing_array = new BitArray(bit_number, default_value == 1);
            }

            public Backing(BitArray bitArray)
            {
                _backing_array = new BitArray(bitArray);
            }

            public static Backing operator <<(Backing n, int shift_num)
            {
                Backing copy = new Backing(n._backing_array);
                copy._backing_array.Length += shift_num;
                copy._backing_array.LeftShift(shift_num);
                return copy;
            }

            public static Backing operator >>(Backing n, int shift_num)
            {
                Backing copy = new Backing(n._backing_array);
                copy._backing_array.RightShift(shift_num);
                copy._backing_array.Length -= shift_num;
                return copy;
            }

            public static Backing operator |(Backing n, Backing n2)
            {
                if (n._backing_array.Count == n2._backing_array.Count){
                    Backing copy = new Backing(n._backing_array);
                    copy._backing_array.Or(n2._backing_array);
                    return copy;
                }
                else if (n._backing_array.Count > n2._backing_array.Count)
                {
                    Backing copy = new Backing(n2._backing_array);
                    copy._backing_array.Length += n._backing_array.Count - n2._backing_array.Count;
                    copy._backing_array.Or(n._backing_array);
                    return copy;
                }
                else
                {
                    Backing copy = new Backing(n._backing_array);
                    copy._backing_array.Length += n2._backing_array.Count - n._backing_array.Count;
                    copy._backing_array.Or(n2._backing_array);
                    return copy;
                }
            }
        
            public static Backing operator &(Backing n, Backing n2)
            {
                if (n._backing_array.Count == n2._backing_array.Count){
                    Backing copy = new Backing(n._backing_array);
                    copy._backing_array.And(n2._backing_array);
                    return copy;
                }
                else if (n._backing_array.Count > n2._backing_array.Count)
                {
                    Backing copy = new Backing(n2._backing_array);
                    copy._backing_array.Length += n._backing_array.Count - n2._backing_array.Count;
                    copy._backing_array.And(n._backing_array);
                    return copy;
                }
                else
                {
                    Backing copy = new Backing(n._backing_array);
                    copy._backing_array.Length += n2._backing_array.Count - n._backing_array.Count;
                    copy._backing_array.And(n2._backing_array);
                    return copy;
                }
            }

            public void SetBit(int index, bool value)
            {
                _backing_array[index] = value;
            }

            public uint GetBit(int index)
            {
                if (_backing_array[index])
                {
                    return 1u;
                }
                else
                {
                    return 0u;
                }
            }

            private static BitArray ToBitArray(object num_obj)
            {
                ulong num = (ulong) num_obj;
                BitArray temp_array = new BitArray(BitConverter.GetBytes(num));
                return temp_array;
            }

            public static Backing operator~(Backing n)
            {
                Backing copy = new Backing(n._backing_array);
                copy._backing_array.Not();
                return copy;
            }


            public uint ToUint()
            {
                byte[] temp_array = new byte[_backing_array.Count];
                _backing_array.CopyTo(temp_array, 0);
                try
                {
                    return BitConverter.ToUInt32(temp_array);
                }
                catch (ArgumentOutOfRangeException)
                {
                    try
                    {
                        return BitConverter.ToUInt16(temp_array);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        try
                        {
                            return (uint) BitConverter.ToSingle(temp_array);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return (uint) (BitConverter.ToBoolean(temp_array) ? 1 : 0);
                        }
                    }
                
                }
        
            }

            public ulong ToULong()
            {
                byte[] temp_array = new byte[_backing_array.Count];
                _backing_array.CopyTo(temp_array, 0);
                return BitConverter.ToUInt64(temp_array);
            }

            public BigInteger ToBigInteger()
            {
                byte[] temp_array = new byte[_backing_array.Count];
                _backing_array.CopyTo(temp_array, 0);
                return new BigInteger(temp_array);
            }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is Backing)
            {
                try
                {
                    return ((Backing)obj).ToULong() == ToULong();
                }
                catch (Exception)
                {
                    return ((Backing)obj).ToBigInteger() == ToBigInteger();
                }
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            try
                {
                    return ToULong().GetHashCode();
                }
            catch (Exception)
                {
                    return ToBigInteger().GetHashCode();
                }
        }
    }



}
