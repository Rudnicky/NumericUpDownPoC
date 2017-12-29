using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NumericUpDownPoC
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl, INotifyPropertyChanged
    {
        #region Properties
        private char _separator;
        #endregion

        #region Dependency Properties
        public decimal Value
        {
            get
            {
                return (decimal)GetValue(ValueProperty);
            }
            set
            {
                if (value <= Maximum && value >= Minimum)
                {
                    SetValue(ValueProperty, Math.Round(value, Precision));
                    OnPropertyChanged("Value");
                }
                else if (value > Maximum)
                {
                    SetValue(ValueProperty, Maximum);
                    OnPropertyChanged("Value");
                }
                else if (value < Minimum)
                {
                    SetValue(ValueProperty, Minimum);
                    OnPropertyChanged("Value");
                }
            }
        }
        public static readonly DependencyProperty ValueProperty =
          DependencyProperty.Register("Value", typeof(decimal),
          typeof(NumericUpDown), new UIPropertyMetadata((decimal)1));

        public decimal Maximum
        {
            get { return (decimal)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum",
                typeof(decimal),
                typeof(NumericUpDown),
                new UIPropertyMetadata((decimal)10));

        public decimal Minimum
        {
            get { return (decimal)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum",
                typeof(decimal),
                typeof(NumericUpDown),
                new UIPropertyMetadata((decimal)0));

        public decimal StepValue
        {
            get { return (decimal)GetValue(StepValueProperty); }
            set { SetValue(StepValueProperty, value); }
        }
        public static readonly DependencyProperty StepValueProperty =
            DependencyProperty.Register("StepValue",
                typeof(decimal),
                typeof(NumericUpDown),
                new UIPropertyMetadata((decimal)0.1));

        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }
        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision",
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata((int)1));

        public bool IsNumericUpDownNA
        {
            get { return (bool)GetValue(IsNumericUpDownNAProperty); }
            set { SetValue(IsNumericUpDownNAProperty, value); }
        }
        public static readonly DependencyProperty IsNumericUpDownNAProperty =
            DependencyProperty.Register("IsNumericUpDownNA",
                typeof(bool),
                typeof(NumericUpDown),
                new PropertyMetadata((false), new PropertyChangedCallback(NumericUpDownNAChanged)));

        public bool IsPercentageModeOn
        {
            get { return (bool)GetValue(IsPercentageModeOnProperty); }
            set { SetValue(IsPercentageModeOnProperty, value); }
        }
        public static readonly DependencyProperty IsPercentageModeOnProperty =
            DependencyProperty.Register("IsPercentageModeOn",
                typeof(bool),
                typeof(NumericUpDown),
                new PropertyMetadata((false), new PropertyChangedCallback(NumericUpDownPercentageChanged)));

        public int DefaultValue
        {
            get { return (int)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue",
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata(0));
        #endregion

        #region Constructor
        public NumericUpDown()
        {
            InitializeComponent();
            CurrentCultureSeparator();
            DefaultBinding();
        }
        #endregion

        #region Events
        private void NumericUpDown_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtBox.Text = DefaultValue.ToString();
            this.Value = DefaultValue;
        }

        private void UpArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Value += StepValue;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void DownArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Value -= StepValue;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TxtBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            bool isPastedTextValid = false;

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pasteText = e.DataObject.GetData(typeof(string)) as string;
                if (!IsNumeric(pasteText))
                    isPastedTextValid = true;
            }

            if (!isPastedTextValid)
                e.CancelCommand();
        }

        private void TxtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.CaretIndex = txtBox.Text.Length;
            }
        }

        private void TxtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == string.Empty)
                {
                    textBox.Text = "0";
                }
            }
        }

        private void TxtBox_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                if (e.Delta > 0)
                {
                    Value += StepValue;
                }
                else if (e.Delta < 0)
                {
                    Value -= StepValue;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TxtBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            if (e.Key == Key.OemPlus)
                e.Handled = true;

            if (Precision <= 1)
            {
                if (e.Key == Key.OemComma)
                    e.Handled = true;
                if (e.Key == Key.OemPeriod)
                    e.Handled = true;
            }

            if (e.Key == Key.Up)
            {
                try
                {
                    this.Value += StepValue;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else if (e.Key == Key.Down)
            {
                try
                {
                    this.Value -= StepValue;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private void TxtBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                e.Handled = IsNumeric(e.Text);
            }
        }

        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var str = ValidateSpecial(txtBox.Text);
            if (ValidateMinuses(str))
            {
                if (str.Length > 0 && str.IndexOf('.') != 0 && str.IndexOf('.') != str.Length - 1)
                {
                    var tmp = Convert.ToDecimal(str);
                    if (tmp > Maximum)
                    {
                        txtBox.Text = Maximum.ToString();
                        txtBox.SelectionStart = str.Length + 1;
                        txtBox.SelectionLength = 0;
                        Value = Maximum;
                    }
                    else if (tmp < Minimum)
                    {
                        txtBox.Text = Minimum.ToString();
                        txtBox.SelectionStart = str.Length + 1;
                        txtBox.SelectionLength = 0;
                        Value = Minimum;
                    }
                    else
                    {

                    }
                }
            }
        }
        #endregion

        #region Methods Private
        private void CurrentCultureSeparator()
        {
            try
            {
                _separator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void DefaultBinding()
        {
            if (!IsNumericUpDownNA || !IsPercentageModeOn)
            {
                Binding binding = new Binding
                {
                    Source = this, // viewmodel of this class
                    Path = new PropertyPath("Value"), // decimal dependency property
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                BindingOperations.SetBinding(txtBox, TextBox.TextProperty, binding);
            }
        }

        private bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, "[^0-9.,-]+");
        }

        private string ValidateSpecial(string str)
        {
            if (str.Contains("%"))
            {
                return str.Substring(0, str.Length - 1);
            }
            else if (str.Contains("N/A"))
            {
                return str.Replace("N/A", "-1");
            }
            return str;
        }

        private bool ValidateMinuses(string str)
        {
            if (str.Length > 0 && str[0] == '-')
            {
                if (str.Length == 1 && str[0] == '-')
                {
                    return false;
                }
                if (HasDuplicate(str, '-'))
                {
                    // if string contains one minus by another
                    // remove second minus and set caret
                    // to the proper position
                    if (str.Length == 2)
                    {
                        txtBox.Text = str.Remove(1, 1);
                        txtBox.CaretIndex = txtBox.Text.Length;
                        return false;
                    }
                    // a little bit logic to remove unwanted minus
                    // instead of removing whole string as it 
                    // used to be.
                    int i = 0;
                    var minusIndex = str.IndexOf('-');
                    int[] indexesOfMinus = new int[str.Length - 1];
                    while (minusIndex != -1)
                    {
                        minusIndex = str.IndexOf('-', minusIndex + 1);
                        indexesOfMinus[i] = minusIndex;
                        i++;
                    }
                    txtBox.Text = str.Remove(indexesOfMinus[0], 1);
                    txtBox.CaretIndex = txtBox.Text.Length;
                    return false;
                }
            }
            return true;
        }

        private bool HasDuplicate(string text, char sign)
        {
            int counter = 0;
            foreach (var ch in text)
            {
                if (ch == sign)
                {
                    counter++;
                }
            }
            if (counter > 1)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region DependencyProperty Callbacks
        private static void NumericUpDownNAChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // this dep method must be a static method
            // in order to set binding with different kind of objects
            // we have to set another non-static callback method
            if (d is NumericUpDown numericUpDown)
            {
                numericUpDown.OnNumericUpDownNAChanged(e);
            }
        }

        private void OnNumericUpDownNAChanged(DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (IsNumericUpDownNA)
                {
                    NotAvailableConverter converter = new NotAvailableConverter();
                    Binding binding = new Binding
                    {
                        Source = this, // viewmodel of this class
                        Path = new PropertyPath("Value"), // decimal dependency property
                        Mode = BindingMode.TwoWay,
                        Converter = converter, // setting up custom converter
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    BindingOperations.SetBinding(txtBox, TextBox.TextProperty, binding);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void NumericUpDownPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown numericUpDown)
            {
                numericUpDown.OnNumericUpDownPercentageChanged(e);
            }
        }

        private void OnNumericUpDownPercentageChanged(DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (IsPercentageModeOn)
                {
                    PercentageConverter converter = new PercentageConverter();
                    Binding binding = new Binding
                    {
                        Source = this, // viewmodel of this class
                        Path = new PropertyPath("Value"), // decimal dependency property
                        Mode = BindingMode.TwoWay,
                        Converter = converter, // setting up custom converter
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    BindingOperations.SetBinding(txtBox, TextBox.TextProperty, binding);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    #region Converters
    [ValueConversion(typeof(decimal), typeof(string))]
    public class NotAvailableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value.ToString();
            if (input != null)
            {
                if (input == "-1")
                {
                    return input.Replace("-1", "N/A");
                }
                else
                {
                    return decimal.Parse(input);
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Regex regex = new Regex(@"[^0-9.,-]+");
                var retValue = value.ToString();
                retValue = regex.Replace(retValue, string.Empty);

                if (retValue == "-")
                {
                    return retValue.Replace("-", "0");
                }
                return retValue;
            }
            catch (Exception)
            {
            }
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(decimal), typeof(string))]
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? null : ((decimal)value).ToString() + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ValidateBackConversion(value.ToString());
        }

        #region Private Methods
        private decimal ValidateBackConversion(string retValue)
        {
            try
            {
                var percentIndex = retValue.IndexOf("%");
                if (retValue.Contains("%"))
                {
                    var length = retValue.Length;
                    var index = retValue.IndexOf('-');

                    if (index == length - 1)
                    {
                        retValue = retValue.Remove(index, 1);
                        retValue = retValue.Insert(0, "-");
                    }

                    if (percentIndex != 0 && percentIndex != -1)
                    {
                        retValue = retValue.Remove(retValue.IndexOf("%", 1));
                    }
                    else
                    {
                        retValue = "0";
                    }

                }

                if (HasDuplicate(retValue, '-'))
                {
                    int i = 0;
                    var minusIndex = retValue.IndexOf('-');
                    int[] indexesOfMinus = new int[retValue.Length - 1];
                    while (minusIndex != -1)
                    {
                        minusIndex = retValue.IndexOf('-', minusIndex + 1);
                        indexesOfMinus[i] = minusIndex;
                        i++;
                    }
                    retValue = retValue.Remove(indexesOfMinus[0], 1);
                }

                if (retValue.Length == 1 && retValue == "-" || retValue == string.Empty)
                {
                    retValue = "0";
                }

                return decimal.Parse(retValue);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private bool HasDuplicate(string text, char sign)
        {
            int counter = 0;
            foreach (var ch in text)
            {
                if (ch == sign)
                {
                    counter++;
                }
            }
            if (counter > 1)
            {
                return true;
            }
            return false;
        }
        #endregion
    }
    #endregion
}
