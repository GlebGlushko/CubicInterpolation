﻿using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MathLibrary
{
    
    public class CubicInterpolation
    {
        private double[] sourceX = null;
        private double[] sourceY = null;
        private long N = 0;
        private double[][] coefs = null;


        /// <summary>
        /// There is Calculate Coefficients of spline
        /// </summary>
        /// <param name="sourceX">The source data OX (abscissa)</param>
        /// <param name="sourceY">The source data OY (ordinate)</param>
        /// <returns>
        /// -1: sourceX.LongLength != sourceY.LongLength
        /// -2: does not enough source data
        /// </returns>
        public int CalcCoefficients(double[] sourceX, double[] sourceY)
        {

            N = sourceX.LongLength;
            if (sourceX.LongLength != sourceY.LongLength)
                return -1;

            if (sourceX.LongLength <= 3)
                return -2;

            this.sourceX = sourceX;
            this.sourceY = sourceY;

            /*
             * Spline[i] = f[i] + b[i]*(x - x[i]) + c[i]*(x - x[i])^2 + d[i]*(x - x[i])^3
             * First: We prepare data for algorithm by calculate dx[i]. If dx[i] equal to zero then function return null.
             * Second: We need calculate coefficients b[i]. 
             * b[i] = 3 * ( (f[i] - f[i - 1])*dx[i]/dx[i - 1] + (f[i + 1] - f[i])*dx[i - 1]/dx[i] ),  i = 1, ... , N - 2
             * How calculate b[0] and b[N - 1] you can see below. And b can be find by means of tridiagonal matrix A[N, N].
             * 
             * A[N, N] - Tridiagonal Matrix:
             *      beta(0)     gama(0)     0            0           0   ...
             *      alfa(1)     beta(1)     gama(1)      0           0   ...
             *      0           alfa(2)     beta(2)     gama(2)      0
             *      ...
             * A*x=b
             * We calculate inverse of tridiagonal matrix by Gauss method and transforming equation A*x=b to the form I*x=b, where I - Identity matrix.
             * Fird: Now we can found coefficients c[i], d[i] where i = 0, ... , N - 2
             */

            long Nx = N - 1;
            double[] dx = new double[Nx];

            double[] b = new double[N];
            double[] alfa = new double[N];
            double[] beta = new double[N];
            double[] gama = new double[N];

            coefs = new double[4][];
            for (long i = 0; i < 4; i++)
                coefs[i] = new double[Nx];

            for (long i = 0; i + 1 <= Nx; i++)
            {
                dx[i] = sourceX[i + 1] - sourceX[i];
                if (dx[i] == 0.0)
                    return -1;
            }

            for (long i = 1; i + 1 <= Nx; i++)
            {
                b[i] = 3.0 * (dx[i] * ((sourceY[i] - sourceY[i - 1]) / dx[i - 1]) + dx[i - 1] * ((sourceY[i + 1] - sourceY[i]) / dx[i]));
            }

            b[0] = ((dx[0] + 2.0 * (sourceX[2] - sourceX[0])) * dx[1] * ((sourceY[1] - sourceY[0]) / dx[0]) +
                        Math.Pow(dx[0], 2.0) * ((sourceY[2] - sourceY[1]) / dx[1])) / (sourceX[2] - sourceX[0]);

            b[N - 1] = (Math.Pow(dx[Nx - 1], 2.0) * ((sourceY[N - 2] - sourceY[N - 3]) / dx[Nx - 2]) + (2.0 * (sourceX[N - 1] - sourceX[N - 3])
                + dx[Nx - 1]) * dx[Nx - 2] * ((sourceY[N - 1] - sourceY[N - 2]) / dx[Nx - 1])) / (sourceX[N - 1] - sourceX[N - 3]);

            beta[0] = dx[1];
            gama[0] = sourceX[2] - sourceX[0];
            beta[N - 1] = dx[Nx - 1];
            alfa[N - 1] = (sourceX[N - 1] - sourceX[N - 3]);
            for (long i = 1; i < N - 1; i++)
            {
                beta[i] = 2.0 * (dx[i] + dx[i - 1]);
                gama[i] = dx[i];
                alfa[i] = dx[i - 1];
            }
            double c = 0.0;
            for (long i = 0; i < N - 1; i++)
            {
                c = beta[i];
                b[i] /= c;
                beta[i] /= c;
                gama[i] /= c;

                c = alfa[i + 1];
                b[i + 1] -= c * b[i];
                alfa[i + 1] -= c * beta[i];
                beta[i + 1] -= c * gama[i];
            }

            b[N - 1] /= beta[N - 1];
            beta[N - 1] = 1.0;
            for (long i = N - 2; i >= 0; i--)
            {
                c = gama[i];
                b[i] -= c * b[i + 1];
                gama[i] -= c * beta[i];
            }

            for (long i = 0; i < Nx; i++)
            {
                double dzzdx = (sourceY[i + 1] - sourceY[i]) / Math.Pow(dx[i], 2.0) - b[i] / dx[i];
                double dzdxdx = b[i + 1] / dx[i] - (sourceY[i + 1] - sourceY[i]) / Math.Pow(dx[i], 2.0);
                coefs[0][i] = (dzdxdx - dzzdx) / dx[i];
                coefs[1][i] = (2.0 * dzzdx - dzdxdx);
                coefs[2][i] = b[i];
                coefs[3][i] = sourceY[i];
            }
            return 0;
        }

        /// <summary> 
        /// I do not recomend use this function when <code>newX > sourceX[N - 1]</code> and <code>newX &lt; sourceX[0]</code>
        /// </summary>
        /// <param name="newX"></param>
        /// <returns></returns>
        public double Interpolate(double newX)
        {
            double newY = 0.0;

            for (long i = 0; i < N; i++)
            {
                if (newX == sourceX[i])
                    return sourceY[i];
            }

            double h = 0.0;

            if (newX < sourceX[0])
            {
                h = newX - sourceX[0];
                newY = coefs[3][0] + h * (coefs[2][0] + h * (coefs[1][0] + h * coefs[0][0] / 3.0) / 2.0);
                return newY;
            }
            if (newX > sourceX[N - 1])
            {
                h = newX - sourceX[N - 1];
                newY = coefs[3][N - 2] + h * (coefs[2][N - 2] + h * (coefs[1][N - 2] + h * coefs[0][N - 2] / 3.0) / 2.0);
                return newY;
            }

            for (long i = 0; i < N - 1; i++)
            {
                if (newX < sourceX[i + 1])
                {
                    h = newX - sourceX[i];
                    newY = coefs[3][i] + h * (coefs[2][i] + h * (coefs[1][i] + h * coefs[0][i] / 3.0) / 2.0);
                    break;
                }
            }
            return newY;
        }

    }
    
    public static class Program
    {
        /// <summary>
        /// CalculateData for interpolation, take left border, right border and step
        /// calculate foreach x in(l,r) with step = step, data x and y = f(x) 
        /// </summary>
        /// <param name="l">left border of interpolation</param>
        /// <param name="r">right border of interpolation</param>
        /// <param name="step">step of interpolation</param>
        /// <returns></returns>
        public static (double[],double[]) calcData(double l, double r, double step) 
        {
            double[] x = new double[(int)((r - l) / step)];
            double[] y = new double[(int)((r - l) / step)];
            int it = 0;
            for (double xi = l; xi <= r; xi += step)
            {
                x[it] = xi;
                y[it++] = F(xi);
            }

            return (x, y);

        }

        public static double F(double x)
        {
            return x * x * Math.Exp(-x * x);
        }
        public static void Main()
        {
            var data = calcData(-2, 0.5, 0.1);
            CubicInterpolation f =new CubicInterpolation();
            f.CalcCoefficients(data.Item1, data.Item2); //calculating coeficients
            Console.WriteLine(f.Interpolate(-1.5)); //calculating value of f(x)
            for (double x = -2; x <= 0.5; x += 0.3)     //calculating interpolation value, and real value of F(x) in [-2, 0.5] with step 0.3
                Console.WriteLine("x = {0:f2},    y = {1:f5},    real y = {2:f5}", x, f.Interpolate(x), F(x));
            Console.ReadKey();
        }
    }
   
}
