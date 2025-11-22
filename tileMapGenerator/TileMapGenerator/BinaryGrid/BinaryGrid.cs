using System.Diagnostics;

namespace BinaryGrid;

// 1 indexed
public class BinaryGrid
{
    internal ulong _grid;
    private (uint, uint) _size;
    public uint RowSize{get{return _size.Item1;}}
    public uint ColSize{get{return _size.Item2;}}
    private uint _border_num = 1;

    public BinaryGrid(uint rows, uint columns, uint borders = 1)
    {
        if (rows == 0 || columns == 0)
        {
            throw new IndexOutOfRangeException();
        }
        _grid = (ulong) Math.Pow(2, (rows+2)*(columns+2)-1)*0;
        _size = (rows, columns);
        if (borders == 1)
        {
            ChangeBorders(borders);
        }
        else if (borders != 0)
        {
            throw new ArgumentException("Value must be 0 or 1");
        }        
    }

    public void ChangeBorders(uint borders)
    {
        if (borders != 1 && borders != 0)
        {
            throw new ArgumentException("Value must be 0 or 1");
        }
        for (uint col = 0; col < _size.Item2+2; col++)
        {
            SetCellInternal(0, col, borders);
            Debug.WriteLine(_grid.ToString("b"));
            SetCellInternal(_size.Item1+1, col, borders);
            Debug.WriteLine(_grid.ToString("b"));
        }
        Debug.WriteLine("transition to rows");
        for (uint row = 0; row < _size.Item1+2; row++)
        {
            SetCellInternal(row, 0, borders);
            Debug.WriteLine(_grid.ToString("b"));
            SetCellInternal(row, _size.Item2+1, borders);
            Debug.WriteLine(_grid.ToString("b"));
        }
        _border_num = borders;
    }

    private ulong GetCellIndex(uint row, uint col){
        return (row)*(_size.Item2+2) + col;
    }

    public void SetCell(uint row, uint col, ulong val)
    {
        if (val != 0 && val != 1)
        {
            throw new ArgumentException("Value must be 0 or 1");
        } 
        else if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
        {
            throw new IndexOutOfRangeException("Bad Cell Index");
        }

        SetCellInternal(row, col, val);
    }

    private void SetCellInternal(uint row, uint col, ulong val)
    {
        if (GetCellInternal(row, col) == 1){
            Debug.WriteLine("adding", ((ulong) Math.Pow(2,GetCellIndex(row, col)) * (val - 1)).ToString("b"));
            _grid += (ulong) Math.Pow(2,GetCellIndex(row, col)) * (val - 1); //add0 or -1
            Debug.WriteLine("set on 1");
        }
        else
        {
            Debug.WriteLine(GetCellIndex(row, col), "cell address");
            Debug.WriteLine("adding", ((ulong) Math.Pow(2,GetCellIndex(row, col)) * val).ToString("b"));
            _grid += (ulong) Math.Pow(2,GetCellIndex(row, col)) * val; //add1 or 0
            Debug.WriteLine("set on 0");
        }
    }

    public ulong GetCell(uint row, uint col)
    {
        if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
        {
            throw new IndexOutOfRangeException("Bad Cell Index");
        }

        return GetCellInternal(row, col);
    }

    private ulong GetCellInternal(uint row, uint col)
    {
        return _grid >> (int) GetCellIndex(row, col) & 1;
    }

    private void InsertEmptyCellInternal(uint row, uint col)
    {
        int cell_index = (int) GetCellIndex(row, col);
        ulong first_half = _grid & ((1UL << cell_index) - 1UL);
        ulong second_half = (_grid << 1) & (~((1UL << (cell_index+1)) - 1UL));
        _grid = first_half | second_half;
    }

    private void DeleteCellInternal(uint row, uint col)
    {
        int cell_index = (int) GetCellIndex(row, col);
        ulong first_half = _grid & ((1UL << cell_index) - 1UL);
        ulong second_half = (_grid >> 1) & (~((1UL << (cell_index-1)) - 1UL));
        _grid = first_half | second_half;
    }

    public ulong GetCellNeighbors(uint row, uint col)
    {
        uint neighbors = 0;
        neighbors = neighbors << 1 | (uint) GetCellInternal(row, col);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row, col+1);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row-1, col+1);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row-1, col);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row-1, col-1);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row, col-1);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row+1, col-1);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row+1, col);
        neighbors = neighbors << 1 | (uint) GetCellInternal(row+1, col+1);
        Debug.WriteLine(neighbors.ToString("b"), "neighbors");
        return neighbors;
    }

    private void CheckIndexValidity(uint row, uint col)
    {
        if (row < 1 || col < 1 || row > _size.Item1 || col > _size.Item2)
        {
            throw new IndexOutOfRangeException("Bad Cell Index");
        }
    }

    private void ValidateNewSize(int size_change_x, int size_change_y)
    {
        if (_size.Item1 + size_change_x <= 0 || _size.Item2 + size_change_y <= 0)
        {
            throw new IndexOutOfRangeException("Graph cannot be smaller than 1 row or column.");
        }
    }

    // need error throwing on these insertion and deletion methods

    public void InsertRow(uint index)
    {
        CheckIndexValidity(index, 1);
        for (int col = 0; col < _size.Item2 + 2; col++)
        {
            InsertEmptyCellInternal(index, (uint) col);
        }
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
        for (int col = (int) _size.Item2+1 ; col >= 0; col--)
        {
            DeleteCellInternal(index, (uint) col);
        }
        _size.Item1--;
    }

    public void DeleteCol(uint index)
    {
        CheckIndexValidity(1, index);
        ValidateNewSize(0, -1);
        for (int row = 0 ; row < _size.Item1+2; row++)
        {
            DeleteCellInternal((uint) row, index);
        }
        _size.Item2--;
    }

    public void SetSlice(uint row1, uint col1, uint row2, uint col2, uint val)
    {
        if (row1 == row2)
        {
            uint min_col = Math.Min(col1, col2);
            uint max_col = Math.Max(col1, col2);

            for (int col = (int) min_col; col <= max_col; col++)
            {
                SetCell(row1, (uint) col, val);
            }
        }
        else if (col1 == col2)
        {
            uint min_row = Math.Min(row1, row2);
            uint max_row = Math.Max(row1, row2);

            for (int row = (int) min_row; row <= max_row; row++)
            {
                SetCell((uint) row, col1, val);
            } 
        }
        else
        {
            throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
        }
    }

    /// <summary>
    ///   Returns min to max indexed
    /// </summary>
    /// <param name="row1"></param>
    /// <param name="col1"></param>
    /// <param name="row2"></param>
    /// <param name="col2"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public ulong GetSlice(uint row1, uint col1, uint row2, uint col2)
    {
        ulong result = 0;
        if (row1 == row2)
        {
            uint min_col = Math.Min(col1, col2);
            uint max_col = Math.Max(col1, col2);

            

            for (int col = (int) min_col; col <= max_col; col++)
            {
                if (col == min_col)
                {
                    result = (uint) GetCell(row1, min_col);
                }
                else
                {
                    result = (result << 1) | (uint) GetCell(row1, (uint) col);
                }
            }
        }
        else if (col1 == col2)
        {
            uint min_row = Math.Min(row1, row2);
            uint max_row = Math.Max(row1, row2);

            for (int row = (int) min_row; row <= max_row; row++)
            {
                if (row == min_row)
                {
                    result = (uint) GetCell((uint) row, col1);
                }
                else
                {
                    result = (result << 1) | (uint) GetCell((uint) row, col1);
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
        uint result = 0;
        if (row1 == row2)
        {
            uint min_col = Math.Min(col1, col2);
            uint max_col = Math.Max(col1, col2);

            for (int col = (int) min_col; col <= max_col; col++)
            {
                result |= (uint) GetCell(row1, (uint) col);
            }
        }
        else if (col1 == col2)
        {
            uint min_row = Math.Min(row1, row2);
            uint max_row = Math.Max(row1, row2);

            for (int row = (int) min_row; row <= max_row; row++)
            {
                result |= (uint) GetCell((uint) row, col1);
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
        uint result = 1;
        if (row1 == row2)
        {
            uint min_col = Math.Min(col1, col2);
            uint max_col = Math.Max(col1, col2);

            for (int col = (int) min_col; col <= max_col; col++)
            {
                result &= (uint) GetCell(row1, (uint) col);
            }
        }
        else if (col1 == col2)
        {
            uint min_row = Math.Min(row1, row2);
            uint max_row = Math.Max(row1, row2);

            for (int row = (int) min_row; row <= max_row; row++)
            {
                result &= (uint) GetCell((uint) row, col1);
            } 
        }
        else
        {
            throw new IndexOutOfRangeException("Slices must be horizontal or vertical");
        }
        return result;
    }

    public uint GetCellOR(uint row, uint col)
    {
        ulong neighbors = 0;
        neighbors |= (uint) GetCellInternal(row, col);
        neighbors |= (uint) GetCellInternal(row, col+1);
        neighbors |= (uint) GetCellInternal(row-1, col+1);
        neighbors |= (uint) GetCellInternal(row-1, col);
        neighbors |= (uint) GetCellInternal(row-1, col-1);
        neighbors |= (uint) GetCellInternal(row, col-1);
        neighbors |= (uint) GetCellInternal(row+1, col-1);
        neighbors |= (uint) GetCellInternal(row+1, col);
        neighbors |= (uint) GetCellInternal(row+1, col+1);
        return (uint) neighbors;
    }

    public uint GetCellAND(uint row, uint col)
    {
        ulong neighbors = 1;
        neighbors &= (uint) GetCellInternal(row, col);
        neighbors &= (uint) GetCellInternal(row, col+1);
        neighbors &= (uint) GetCellInternal(row-1, col+1);
        neighbors &= (uint) GetCellInternal(row-1, col);
        neighbors &= (uint) GetCellInternal(row-1, col-1);
        neighbors &= (uint) GetCellInternal(row, col-1);
        neighbors &= (uint) GetCellInternal(row+1, col-1);
        neighbors &= (uint) GetCellInternal(row+1, col);
        neighbors &= (uint) GetCellInternal(row+1, col+1);
        return (uint) neighbors;
    }

    public override bool Equals(object? obj)
    {
        if (obj != null && obj is BinaryGrid)
        {
            return ((BinaryGrid) obj)._grid == _grid;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return _grid.GetHashCode();
    }
}


