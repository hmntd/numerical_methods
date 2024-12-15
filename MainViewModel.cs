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
        public decimal A { get; set; }
        public decimal B { get; set; }
        public decimal Step { get; set; }
        public decimal ReferenceValue { get; set; }
        public int Counter { get; set; } = 0;
        public bool IsCalculating { get; set; } = true;
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
                IsCalculating = false;
                OnPropertyChanged(nameof(IsCalculating));

                if (B < A || Step <= 0 || Step > (B - A))
                {
                    MessageBox.Show("Функція має недопустимі значення", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Results.Clear();

                var methods = new[] { "Ліві прямокутники", "Праві прямокутники", "Середні прямокутники", "Трапеції", "Сімпсона" };

                var tasks = methods.Select(async method =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    decimal integral = await Task.Run(() => CalculateIntegral(method));
                    stopwatch.Stop();

                    decimal error = Math.Abs(integral - ReferenceValue) / ReferenceValue * 100;

                    return new Result
                    {
                        Method = method,
                        ElementCount = (int)((B - A) / Step),
                        Time = stopwatch.ElapsedMilliseconds,
                        ResultIntegral = integral,
                        ErrorPercent = error
                    };
                });

                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    Results.Add(result);
                }

                OnPropertyChanged(nameof(Results));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsCalculating = true;
                OnPropertyChanged(nameof(IsCalculating));
            }
        }
        private IEnumerable<decimal> GenerateSequence(decimal start, decimal end, decimal step)
        {
            int precision = GetDecimalPlaces(step);
            for (decimal value = start; value <= end; value += step)
            {
                yield return Math.Round(value, precision);
            }
        }

        private int GetDecimalPlaces(decimal value)
        {
            string valueStr = value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
            int decimalIndex = valueStr.IndexOf('.');
            return decimalIndex >= 0 ? valueStr.Length - decimalIndex - 1 : 0;
        }

        private bool IsValid(string function, decimal x)
        {
            try
            {
                decimal result = Evaluate(function, x);
                return result != decimal.MaxValue || result != decimal.MinValue;
            }
            catch
            {
                return false;
            }
        }

        private decimal CalculateIntegral(string method)
        {
            decimal result = 0;
            decimal previousValue = 0;

            switch (method)
            {
                case "Ліві прямокутники":
                    for (decimal x = A; x <= B - Step; x += Step)
                    {
                        result += Evaluate(Function, x) * Step;
                        Counter++;
                        OnPropertyChanged(nameof(Counter));
                    }
                    break;
                case "Праві прямокутники":
                    for (decimal x = A + Step; x <= B; x += Step)
                    {
                        result += Evaluate(Function, x) * Step;
                        Counter++;
                        OnPropertyChanged(nameof(Counter));
                    }
                    break;
                case "Середні прямокутники":
                    for (decimal x = A; x <= B - Step; x += Step)
                    {
                        result += Evaluate(Function, x + Step / 2) * Step;
                        Counter++;
                        OnPropertyChanged(nameof(Counter));
                    }
                    break;
                case "Трапеції":
                    previousValue = Evaluate(Function, A);
                    for (decimal x = A + Step; x <= B; x += Step)
                    {
                        var currentValue = Evaluate(Function, x);
                        result += (previousValue + currentValue) / 2 * Step;
                        previousValue = currentValue;
                        Counter++;
                        OnPropertyChanged(nameof(Counter));
                    }
                    break;
                case "Сімпсона":
                    result = Evaluate(Function, A) + Evaluate(Function, B);
                    int index = 1;
                    for (decimal x = A + Step; x < B; x += Step, index++)
                    {
                        result += (index % 2 == 0 ? 2 : 4) * Evaluate(Function, x);
                        Counter++;
                        OnPropertyChanged(nameof(Counter));
                    }
                    result *= Step / 3;
                    break;
            }

            return result;
        }


        private decimal Evaluate(string function, decimal x)
        {
            try
            {
                var parsedExpression = Infix.ParseOrThrow(function);
                var symbolValues = new Dictionary<string, FloatingPoint>
        {
            { "x", (FloatingPoint)(double)x }
        };
                var result = MathNet.Symbolics.Evaluate.Evaluate(symbolValues, parsedExpression);

                if (result is FloatingPoint floatingPointResult)
                {
                    decimal realValue = (decimal)floatingPointResult.RealValue;

                    if (realValue == decimal.MaxValue || realValue == decimal.MinValue)
                        throw new Exception("Результат є недійсним числом (Zero, MaxValue, MinValue).");

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
