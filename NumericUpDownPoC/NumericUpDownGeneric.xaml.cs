using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NumericUpDownPoC
{
    /// <summary>
    /// Interaction logic for NumericUpDownGeneric.xaml
    /// </summary>
    public partial class NumericUpDownGeneric : UserControl, INotifyPropertyChanged
    {
        #region Constant Properties
        private const string DefaultValue = "0";
        private const string NotAvailable = "N/A";
        #endregion

        #region Dependency Properties
        private char _sign;

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
          typeof(NumericUpDownGeneric), new UIPropertyMetadata((decimal)1));

        public decimal Maximum
        {
            get { return (decimal)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum",
                typeof(decimal),
                typeof(NumericUpDownGeneric),
                new UIPropertyMetadata((decimal)10));

        public decimal Minimum
        {
            get { return (decimal)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum",
                typeof(decimal),
                typeof(NumericUpDownGeneric),
                new UIPropertyMetadata((decimal)0));

        public decimal StepValue
        {
            get { return (decimal)GetValue(StepValueProperty); }
            set { SetValue(StepValueProperty, value); }
        }

        public static readonly DependencyProperty StepValueProperty =
            DependencyProperty.Register("StepValue",
                typeof(decimal),
                typeof(NumericUpDownGeneric),
                new UIPropertyMetadata((decimal)0.1));

        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision",
                typeof(int),
                typeof(NumericUpDownGeneric),
                new PropertyMetadata((int)1));

        public bool IsNumericUpDownNA
        {
            get { return (bool)GetValue(IsNumericUpDownNAProperty); }
            set { SetValue(IsNumericUpDownNAProperty, value); }
        }

        public static readonly DependencyProperty IsNumericUpDownNAProperty =
            DependencyProperty.Register("IsNumericUpDownNA",
                typeof(bool),
                typeof(NumericUpDownGeneric),
                new PropertyMetadata(false));
        #endregion

        #region Constructor
        public NumericUpDownGeneric()
        {
            InitializeComponent();
            _sign = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }
        #endregion

        #region Events Realisation
        private void UpArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Value += StepValue;
                this.txtBox.Text = Value.ToString();
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
                this.txtBox.Text = Value.ToString();
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

        private void TxtBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                e.Handled = IsNumeric(e.Text);
            }
        }

        private void TxtBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // is space's clicked return
            if (e.Key == Key.Space)
                e.Handled = true;

            // if plus's clicked return
            if (e.Key == Key.OemPlus)
                e.Handled = true;

            // if there's not precision
            if (Precision <= 1)
            {
                // if ,
                if (e.Key == Key.OemComma)
                    e.Handled = true;

                // if .
                if (e.Key == Key.OemPeriod)
                    e.Handled = true;
            }
        }

        private void TxtBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                Value = Convert.ToDecimal(txtBox.Text.ToString());
                if (e.Delta > 0)
                    Value += StepValue;
                else if (e.Delta < 0)
                    Value -= StepValue;
            }
            catch (Exception ex)
            {

            }
        }

        private void TxtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == string.Empty || textBox.Text == "." || textBox.Text == "," || textBox.Text == "-1" || textBox.Text == "-")
                {
                    if (!IsNumericUpDownNA)
                    {
                        textBox.Text = DefaultValue;
                    }
                    else
                    {
                        textBox.Text = NotAvailable;
                    }
                }
                else
                {
                    textBox.Text = WorkWithComma(textBox.Text);
                }

                var str = textBox.Text;
                if (str.Length > 1 && (str[str.Length - 1] == '.' || str[str.Length - 1] == ','))
                {
                    textBox.Text =  str.Remove(str.Length - 1);
                }
            }
        }

        private void TxtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!IsNumericUpDownNA)
                {
                    if (textBox.Text == DefaultValue)
                    {
                        textBox.Text = string.Empty;
                    }
                }
                else
                {
                    if (textBox.Text == NotAvailable)
                    {
                        textBox.Text = string.Empty;
                    }
                }
            }
        }

        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtBox = sender as TextBox;
            if (txtBox != null)
            {
                var newTextValue = txtBox.Text;
                if (newTextValue != NotAvailable)
                {
                    if (newTextValue.Contains(NotAvailable))
                    {
                        GeneralValidation(RemoveDuplicate(newTextValue));
                    }
                    else
                    {
                        GeneralValidation(newTextValue);
                    }
                }
            }
        }
        #endregion

        #region Methods Private
        private void GeneralValidation(string str)
        {
            if (str.Contains(","))
            {
                str = str.Replace(",", ".");
            }

            if (IsValueValid(str))
            {
                str = WorkWithComma(str);

                if (str.Length > 0 && str.IndexOf('.') != 0 && str.IndexOf('.') != str.Length - 1)
                {
                    decimal TempValue = Convert.ToDecimal(str);

                    if (TempValue > Maximum)
                    {
                        txtBox.Text = Maximum.ToString();
                        txtBox.SelectionStart = str.Length + 1;
                        txtBox.SelectionLength = 0;
                        Value = Maximum;
                    }
                    else if (TempValue < Minimum)
                    {
                        txtBox.Text = Minimum.ToString();
                        txtBox.SelectionStart = str.Length + 1;
                        txtBox.SelectionLength = 0;
                        Value = Minimum;
                    }
                    else
                    {
                        int selectionIndex = txtBox.SelectionStart;
                        txtBox.Text = Math.Round((Convert.ToDecimal(str) > Maximum) ? Maximum : Convert.ToDecimal(str), Precision).ToString();
                        txtBox.SelectionStart = selectionIndex;
                        Value = Math.Round((Convert.ToDecimal(str) > Maximum) ? Maximum : Convert.ToDecimal(str), Precision);
                    }
                }
            }
        }

        private bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, "[^0-9.,-]+");
        }

        private bool IsValueValid(string str)
        {
            if (str.Length > 0 && str[0] == '-')
            {
                if (str.Length == 1 && str[0] == '-')
                {
                    return false;
                }

                if (HasDuplicate(str, '-'))
                {
                    txtBox.Text = string.Empty;
                    return false;
                }
            }

            if (HasDuplicate(str, '.') || HasDuplicate(str, ','))
            {
                txtBox.Text = string.Empty;
                return false;
            }

            if (str != "," && !string.IsNullOrEmpty(str))
            {
                return true;
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

        private string RemoveDuplicate(string str)
        {
            if (str.Contains(NotAvailable))
            {
                return str.Replace(NotAvailable, string.Empty);
            }
            else
            {
                return string.Empty;
            }
        }

        private string WorkWithComma(string str)
        {
            if (str.Length > 1 && (str[0] == '.' || str[0] == ','))
            {
                return str.Insert(0, "0");
            }
            //else if (str.Length > 1 && (str[0] != ',') || (str[0] != ','))
            //{
            //    char lastCharacter = str[str.Length - 1];
            //    if (lastCharacter == ',' || lastCharacter == '.')
            //    {
            //        return str.Remove(str.Length - 1);
            //    }
            //}
            return str;
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
}
