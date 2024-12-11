using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MathNet.Symbolics;

namespace numerical_methods
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string Function { get; set; }
        public double A { get; set; }
        public double B { get; set; }
        public double Step { get; set; }
        public double ReferenceValue { get; set; }
        public int Counter { get; set; } = 0;
        public ObservableCollection<Result> Results { get; set; } = new ObservableCollection<Result>();
        public ICommand Calculate_Click { get; }
        public MainViewModel()
        {
            Calculate_Click = new RelayCommand(Calculate);
        }
        private async void Calculate()
        {
            try
            {
                if (B < A || Step < 0 || Step > (B - A))
                {
                    MessageBox.Show("Функція має недопустимі значення", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                var points = Enumerable.Range(0, (int)((B - A) / Step) + 1)
                       .Select(i => A + i * Step)
                       .ToArray();

                if (points.Any(x => !IsValid(Function, x)))
                {
                    MessageBox.Show("Функція має недопустиме значення в межах існування.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Results.Clear();

                var methods = new[] { "Ліві прямокутники", "Праві прямокутники", "Середні прямокутники", "Трапеції", "Сімпсона" };

                var tasks = methods.Select(async method =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    double integral = await Task.Run(() => CalculateIntegral(method, points));
                    stopwatch.Stop();

                    double error = Math.Abs(integral - ReferenceValue) / ReferenceValue * 100;

                    return new Result
                    {
                        Method = method,
                        ElementCount = points.Length,
                        Time = stopwatch.ElapsedMilliseconds,
                        ErrorPercent = error
                    };
                });

                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    Results.Add(result);
                }

                OnPropertyChanged(nameof(Results));
            } catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValid(string function, double x)
        {
            try
            {
                double result = Evaluate(function, x);
                return !double.IsNaN(result) && !double.IsInfinity(result);
            }
            catch
            {
                return false;
            }
        }
        private double CalculateIntegral(string method, double[] points)
        {
            double result = 0;

            switch (method)
            {
                case "Ліві прямокутники":
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        result += Evaluate(Function, points[i]) * Step;
                        Counter++;
                    }
                    OnPropertyChanged(nameof(Counter));
                    break;
                case "Праві прямокутники":
                    for (int i = 1; i < points.Length; i++)
                    {
                        result += Evaluate(Function, points[i]) * Step;
                        Counter++;
                    }
                    break;
                case "Середні прямокутники":
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        result += Evaluate(Function, (points[i] + points[i + 1]) / 2) * Step;
                        Counter++;
                    }
                    OnPropertyChanged(nameof(Counter));
                    break;
                case "Трапеції":
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        result += (Evaluate(Function, points[i]) + Evaluate(Function, points[i + 1])) / 2 * Step;
                        Counter++;
                    }
                    OnPropertyChanged(nameof(Counter));
                    break;
                case "Сімпсона":
                    for (int i = 0; i < points.Length - 1; i += 2)
                    {
                        if (i + 2 < points.Length)
                        {
                            double h = (points[i + 2] - points[i]) / 2;
                            result += (h / 3) * (Evaluate(Function, points[i]) + 4 * Evaluate(Function, points[i + 1]) + Evaluate(Function, points[i + 2]));
                        }
                        Counter++;
                    }
                    OnPropertyChanged(nameof(Counter));
                    break;
            }
            return result;
        }
        private double Evaluate(string function, double x)
        {
            try
            {
                var parsedExpression = Infix.ParseOrThrow(function);

                var symbolValues = new Dictionary<string, FloatingPoint>
        {
            { "x", (FloatingPoint)x }
        };
                var result = MathNet.Symbolics.Evaluate.Evaluate(symbolValues, parsedExpression);

                if (result is FloatingPoint floatingPointResult)
                {
                    double realValue = floatingPointResult.RealValue;
                    if (double.IsNaN(realValue) || double.IsInfinity(realValue))
                        throw new Exception("Результат є недійсним числом (NaN або Infinity).");

                    return realValue;
                }

                throw new Exception("Обчислення не дало дійсного результату.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка при обчисленні функції: {ex.Message}");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
