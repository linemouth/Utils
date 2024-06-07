using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public class Matrix : IEquatable<Matrix>, ICloneable, IEnumerable<double>
    {
        public double this[int r, int c] { get => m[r, c]; set => m[r, c] = value; }
        public int RowCount => m.GetLength(0);
        public int ColumnCount => m.GetLength(1);
        public int Rank => RowCount == ColumnCount ? RowCount : -1;

        protected double[,] m;

        private int currentRow = 0;

        public Matrix(int dimension) : this(dimension, dimension) { }
        public Matrix(int rowCount, int columnCount) : this(new double[rowCount, columnCount]) { }
        public Matrix(double[,] values)
        {
            m = (double[,])values.Clone();
        }
        public Matrix(int rowCount, int columnCount, params double[] values)
        {
            if(values.Length != rowCount * columnCount)
            {
                throw new ArgumentException($"Matrix initializer list must contain {rowCount * columnCount} values to fill the {rowCount}x{columnCount} matrix.");
            }
            m = new double[rowCount, columnCount];
            for(int r = 0, i = 0; r < rowCount; ++r)
            {
                for(int c = 0; c < columnCount; ++c, ++i)
                {
                    m[r, c] = values[i];
                }
            }
        }
        public static Matrix FromRow(params double[] values) => new Matrix(1, values.Length, values);
        public static Matrix FromColumn(params double[] values) => new Matrix(values.Length, 1, values);
        public static Matrix GetIdentity(int rowCount, int columnCount)
        {
            Matrix matrix = new Matrix(rowCount, columnCount);
            for(int i = 0; i < Math.Min(rowCount, columnCount); ++i)
            {
                matrix.m[i, i] = 1;
            }
            return matrix;
        }
        public static Matrix Cross(Matrix a, Matrix b)
        {
            if(a.ColumnCount != b.RowCount)
            {
                throw new ArithmeticException($"Cannot cross multiply {a.RowCount}x{a.ColumnCount} and {b.RowCount}x{b.ColumnCount} matrices.");
            }
            int size = a.ColumnCount;
            Matrix result = new Matrix(a.RowCount, b.ColumnCount);

            for(var r = 0; r < result.RowCount; ++r)
            {
                for(var c = 0; c < result.ColumnCount; ++c)
                {
                    double value = 0;
                    for(var i = 0; i < size; ++i)
                    {
                        value += a[r, i] * b[i, c];
                    }
                    result[r, c] = value;
                }
            }
            return result;
        }
        public static Matrix operator *(Matrix a, Matrix b) => Cross(a, b);
        public bool Equals(Matrix other) => Equals(m, other.m);
        public object Clone() => new Matrix(m);
        public void Add(params double[] values)
        {
            for(int c = 0; c < values.Length; ++c)
            {
                m[currentRow, c] = values[c];
            }
            ++currentRow;
        }
        public IEnumerator<double> GetEnumerator() => (IEnumerator<double>)m.GetEnumerator();
        public double[] GetRow(int index) => Enumerable.Range(0, ColumnCount).Select(c => m[index, c]).ToArray();
        public double[] GetColumn(int index) => Enumerable.Range(0, RowCount).Select(r => m[r, index]).ToArray();
        public double[][] GetRows()
        {
            double[][] rows = new double[RowCount][];
            for(int r = 0; r < RowCount; ++r)
            {
                rows[r] = GetRow(r);
            }
            return rows;
        }
        public double[][] GetColumns()
        {
            double[][] columns = new double[ColumnCount][];
            for(int c = 0; c < ColumnCount; ++c)
            {
                columns[c] = GetRow(c);
            }
            return columns;
        }
        public override string ToString() => $"{RowCount}x{ColumnCount} Matrix: [{string.Join("|", GetRows().Select(row => string.Join(" ", row.Select(value => $"{value:F3}"))))}]";
        public double[] ToArray()
        {
            double[] array = new double[m.Length];
            Buffer.BlockCopy(m, 0, array, 0, m.Length);
            return array;
        }
        public Matrix Inverse()
        {
            if(RowCount != ColumnCount)
            {
                throw new ArithmeticException($"Cannot invert a non-square {RowCount}x{ColumnCount} matrix.");
            }
            int size = RowCount;
            Matrix result = GetIdentity(size, size);
            Matrix temp = (Matrix)Clone();

            // Perform elementary row operations
            for(var r = 0; r < size; r += 1)
            {
                // get the element on the diagonal
                double diagonal = temp[r, r];

                // If we have a 0 on the diagonal (we'll need to swap with a lower row)
                if(diagonal == 0)
                {
                    // Look through every row below the current row
                    for(var r2 = r + 1; r2 < size; r2 += 1)
                    {
                        // If the other row has a non-0 in the current column
                        if(temp[r2, r] != 0)
                        {
                            // It would make the diagonal have a non-0. Swap it!
                            for(var c = 0; c < size; c++)
                            {
                                // For elements in Copy
                                diagonal = temp[r, c];                // Temp store current
                                temp[r, c] = temp[r2, c];    // Replace current by other
                                temp[r2, c] = diagonal;                // Repace other by temp

                                // For elements in Identity
                                diagonal = result[r, c];                // Temp store current
                                result[r, c] = result[r2, c];    // Replace current by other
                                result[r2, c] = diagonal;                // Repace other by temp
                            }
                            // Don't bother checking other rows since we've swapped
                            break;
                        }
                    }
                    // Get the new diagonal
                    diagonal = temp[r, r];
                }

                // If the diagonal is still zero, then it can't be inverted.
                if(diagonal == 0)
                {
                    throw new ArithmeticException("Cannot invert matrix with zero on diagonal.");
                }

                // Scale this row down by e (so we have a 1 on the diagonal)
                for(var c = 0; c < size; c++)
                {
                    temp[r, c] = temp[r, c] / diagonal;    // Apply to original matrix
                    result[r, c] = result[r, c] / diagonal;    // Apply to identity
                }

                // Subtract this row (scaled appropriately for each row) from ALL of
                // the other rows so that there will be 0's in this column in the
                // rows above and below this one
                for(var r2 = 0; r2 < size; r2++)
                {
                    // Only apply to other rows (we want a 1 on the diagonal)
                    if(r2 == r)
                    {
                        continue;
                    }

                    // We want to change this element to 0
                    diagonal = temp[r2, r];

                    // Subtract (the row above(or below) scaled by e) from (the
                    // current row) but start at the current column and assume all the
                    // stuff left of diagonal is 0 (which it should be if we made this
                    // algorithm correctly)
                    for(var c = 0; c < size; c++)
                    {
                        temp[r2, c] -= diagonal * temp[r, c];    // Apply to original matrix
                        result[r2, c] -= diagonal * result[r, c];    // Apply to identity
                    }
                }
            }

            // We've done all operations, C should be the identity
            // Let's check our work to be sure
            bool ok = true;
            for(int r = 0; r < size && ok; ++r)
            {
                for(int c = 0; c < size && ok; ++c)
                {
                    ok = temp[r, c] == ((r == c) ? 1 : 0);
                }
            }
            if(ok)
            {
                return result;
            }
            else
            {
                throw new ArithmeticException("Failed to invert matrix.");
            }
        }
        public Matrix Transposed()
        {
            Matrix result = new Matrix(ColumnCount, RowCount);
            for(int r = 0; r < RowCount; ++r)
            {
                for(int c = 0; c < ColumnCount; ++c)
                {
                    result[c, r] = m[r, c];
                }
            }
            return result;
        }
        public Matrix SymmetricFactorizationMatrix() => Transposed() * this;
        public Matrix Minor(int row, int column)
        {
            Matrix submatrix = new Matrix(RowCount - 1, ColumnCount - 1);
            for(int r = 0; r < submatrix.RowCount; ++r)
            {
                int i = r < row ? r : r + 1;
                for(int c = 0; c < submatrix.ColumnCount; ++c)
                {
                    int j = r < row ? r : r + 1;
                    submatrix[r, c] = this[i, j];
                }
            }
            return submatrix;
        }
        public double Determinant()
        {
            int rank = Rank;
            double determinant = 0;
            if(rank <= 0)
            {
                throw new ArithmeticException($"{RowCount}x{ColumnCount} matrix does not have a determinant.");
            }
            else if(rank == 1)
            {
                determinant = m[0, 0];
            }
            else if(rank == 2)
            {
                determinant = m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0];
            }
            else
            {
                for(int c = 0; c < rank; ++c)
                {
                    double d = Minor(0, c).Determinant();
                    determinant += (c & 0x01) == 0 ? d : -d;
                }
            }
            return determinant;
        }
        IEnumerator IEnumerable.GetEnumerator() => m.GetEnumerator();
        /*public void SingularValueDecomposition(out Matrix u, out double[] s, out Matrix v)
        {
            SingularValueDecomposition(out u, out Matrix S, out v);
            s = new double[S.Rank];
            for(int i = 0; i < s.Length; ++i)
            {
                s[i] = S[i, i];
            }
        }
        public void SingularValueDecomposition(out Matrix u, out Matrix s, out Matrix v)
        {
            Matrix AtA = SymmetricFactorizationMatrix();

        }*/
        public void Decompose(out Matrix lower, out Matrix upper)
        {
            if(RowCount != ColumnCount)
            {
                throw new ArithmeticException($"Cannot decompose a non-square {RowCount}x{ColumnCount} matrix.");
            }
            int size = RowCount;
            lower = new Matrix(size);
            upper = new Matrix(size);

            for(int i = 0; i < size; i++)
            {
                for(int j = 0; j < size; j++)
                {
                    if(j >= i)
                    {
                        lower[j, i] = m[j, i];
                        for(int k = 0; k < i; k++)
                        {
                            lower[j, i] = lower[j, i] - lower[j, k] * upper[k, i];
                        }
                    }
                    /*else
                    {
                        lower[j, i] = 0; // It's already initialized to zero
                    }*/
                }
                for(int j = 0; j < size; j++)
                {
                    if(j == i)
                    {
                        upper[i, j] = 1;
                    }
                    else if(j > i)
                    {
                        upper[i, j] = m[i, j] / lower[i, i];
                        for(int k = 0; k < i; k++)
                        {
                            upper[i, j] = upper[i, j] - ((lower[i, k] * upper[k, j]) / lower[i, i]);
                        }
                    }
                    /*else // j < i
                    {
                        upper[i, j] = 0; // It's already initialized to zero
                    }*/
                }
            }
        }
        /// <summary>Solves systems of equations in the form Ax = b</summary>
        /// <param name="b">The vector of equation constants.</param>
        /// <returns>The vector of solutions.</returns>
        public double[] Solve(params double[] b)
        {
            if(ColumnCount != b.Count())
            {
                throw new ArithmeticException($"Cannot solve system of {b.Count()} expressions with {RowCount}x{ColumnCount} matrix.");
            }

            /* Using inverse
             * 1. Ax = b
             * 2. A`Ax = A`b = x
             */
            Matrix inverse = Inverse();
            Matrix result = inverse * FromColumn(b);
            return result.ToArray();

            /* Using LU decomposition:
             * 1. Ax = b
             * 2. A`Ax = A`b = x
             * 3. LUx = b
             * 4. Ux = Y
             * 5. Ly = b, solve for y
             * 6. Ux = y, solve for x

            // First, decompose the matrix into lower and upper triangular matrices.
            Decompose(out Matrix lower, out Matrix upper);

            // Second, solve the system LY = b for Y
            double[] y = new double[size];

            // Lastly, solve the system Ux = Y for x*/
        }
    
        /*private static void Householders_Reduction_to_Bidiagonal_Form(Matrix A, int nrows, int ncols, double* U, double* V, double* diagonal, double* superdiagonal)
        {
            int i, j, k, ip1;
            double s, s2, si, scale;
            double dum;
            double* pu, *pui, *pv, *pvi;
            double half_norm_squared;

            // Copy A to U

            memcpy(U, A, sizeof(double) * nrows * ncols);

            //

            diagonal[0] = 0.0;
            s = 0.0;
            scale = 0.0;
            for(i = 0, pui = U, ip1 = 1; i < ncols; pui += ncols, i++, ip1++)
            {
                superdiagonal[i] = scale * s;
                //       
                //                  Perform Householder transform on columns.
                //
                //       Calculate the normed squared of the i-th column vector starting at 
                //       row i.
                //
                for(j = i, pu = pui, scale = 0.0; j < nrows; j++, pu += ncols)
                    scale += fabs(*(pu + i));

                if(scale > 0.0)
                {
                    for(j = i, pu = pui, s2 = 0.0; j < nrows; j++, pu += ncols)
                    {
                        *(pu + i) /= scale;
                        s2 += *(pu + i) * *(pu + i);
                    }
                    //
                    //    
                    //       Chose sign of s which maximizes the norm
                    //  
                    s = (*(pui + i) < 0.0) ? sqrt(s2) : -sqrt(s2);
                    //
                    //       Calculate -2/u'u
                    //
                    half_norm_squared = *(pui + i) * s - s2;
                    //
                    //       Transform remaining columns by the Householder transform.
                    //
                    *(pui + i) -= s;

                    for(j = ip1; j < ncols; j++)
                    {
                        for(k = i, si = 0.0, pu = pui; k < nrows; k++, pu += ncols)
                            si += *(pu + i) * *(pu + j);
                        si /= half_norm_squared;
                        for(k = i, pu = pui; k < nrows; k++, pu += ncols)
                        {
                            *(pu + j) += si * *(pu + i);
                        }
                    }
                }
                for(j = i, pu = pui; j < nrows; j++, pu += ncols) *(pu + i) *= scale;
                diagonal[i] = s * scale;
                //       
                //                  Perform Householder transform on rows.
                //
                //       Calculate the normed squared of the i-th row vector starting at 
                //       column i.
                //
                s = 0.0;
                scale = 0.0;
                if(i >= nrows || i == (ncols - 1)) continue;
                for(j = ip1; j < ncols; j++) scale += fabs(*(pui + j));
                if(scale > 0.0)
                {
                    for(j = ip1, s2 = 0.0; j < ncols; j++)
                    {
                        *(pui + j) /= scale;
                        s2 += *(pui + j) * *(pui + j);
                    }
                    s = (*(pui + ip1) < 0.0) ? sqrt(s2) : -sqrt(s2);
                    //
                    //       Calculate -2/u'u
                    //
                    half_norm_squared = *(pui + ip1) * s - s2;
                    //
                    //       Transform the rows by the Householder transform.
                    //
                    *(pui + ip1) -= s;
                    for(k = ip1; k < ncols; k++)
                        superdiagonal[k] = *(pui + k) / half_norm_squared;
                    if(i < (nrows - 1))
                    {
                        for(j = ip1, pu = pui + ncols; j < nrows; j++, pu += ncols)
                        {
                            for(k = ip1, si = 0.0; k < ncols; k++)
                                si += *(pui + k) * *(pu + k);
                            for(k = ip1; k < ncols; k++)
                            {
                                *(pu + k) += si * superdiagonal[k];
                            }
                        }
                    }
                    for(k = ip1; k < ncols; k++) *(pui + k) *= scale;
                }
            }

            // Update V
            pui = U + ncols * (ncols - 2);
            pvi = V + ncols * (ncols - 1);
            *(pvi + ncols - 1) = 1.0;
            s = superdiagonal[ncols - 1];
            pvi -= ncols;
            for(i = ncols - 2, ip1 = ncols - 1; i >= 0; i--, pui -= ncols,
                                                               pvi -= ncols, ip1--)
            {
                if(s != 0.0)
                {
                    pv = pvi + ncols;
                    for(j = ip1; j < ncols; j++, pv += ncols)
                        *(pv + i) = (*(pui + j) / *(pui + ip1)) / s;
                    for(j = ip1; j < ncols; j++)
                    {
                        si = 0.0;
                        for(k = ip1, pv = pvi + ncols; k < ncols; k++, pv += ncols)
                            si += *(pui + k) * *(pv + j);
                        for(k = ip1, pv = pvi + ncols; k < ncols; k++, pv += ncols)
                            *(pv + j) += si * *(pv + i);
                    }
                }
                pv = pvi + ncols;
                for(j = ip1; j < ncols; j++, pv += ncols)
                {
                    *(pvi + j) = 0.0;
                    *(pv + i) = 0.0;
                }
                *(pvi + i) = 1.0;
                s = superdiagonal[i];
            }

            // Update U

            pui = U + ncols * (ncols - 1);
            for(i = ncols - 1, ip1 = ncols; i >= 0; ip1 = i, i--, pui -= ncols)
            {
                s = diagonal[i];
                for(j = ip1; j < ncols; j++) *(pui + j) = 0.0;
                if(s != 0.0)
                {
                    for(j = ip1; j < ncols; j++)
                    {
                        si = 0.0;
                        pu = pui + ncols;
                        for(k = ip1; k < nrows; k++, pu += ncols)
                            si += *(pu + i) * *(pu + j);
                        si = (si / *(pui + i)) / s;
                        for(k = i, pu = pui; k < nrows; k++, pu += ncols)
                            *(pu + j) += si * *(pu + i);
                    }
                    for(j = i, pu = pui; j < nrows; j++, pu += ncols)
                    {
                        *(pu + i) /= s;
                    }
                }
                else
                    for(j = i, pu = pui; j < nrows; j++, pu += ncols) *(pu + i) = 0.0;
                *(pui + i) += 1.0;
            }
        }*/
    }
}
