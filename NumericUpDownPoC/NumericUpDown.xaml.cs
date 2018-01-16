using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

// try to change decimal parse 
// and add converter of dots and commas

namespace NumericUpDownPoC
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl, INotifyPropertyChanged
    {
        #region Properties
        private char _currentCultureSeparator;
        private CultureInfo _currentCulture;
        private const string Dot = ".";
        private const string Comma = ",";
        private const string Minus = "-";
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
                SetValue(ValueProperty, value);
                OnPropertyChanged("Value");
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
                new UIPropertyMetadata((decimal)0.5));

        public decimal DefaultValue
        {
            get { return (decimal)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue",
                typeof(decimal),
                typeof(NumericUpDown),
                new PropertyMetadata((decimal)0));

        public int Precision
        {
            get { return (int)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }
        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision",
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata(0));

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
        #endregion

        #region Constructor
        public NumericUpDown()
        {
            InitializeComponent();
            SetupInvariantCulture();

            this.DataContext = this;
        }
        #endregion

        #region Events
        private void TxtBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                bool isPastedTextValid = false;

                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    var pasteText = e.DataObject.GetData(typeof(string)) as string;
                    if (pasteText.Contains(Comma))
                    {
                        pasteText = pasteText.Replace(Comma, Dot);
                    }

                    if (IsNumericWithDot(pasteText))
                    {
                        isPastedTextValid = true;

                        var inputText = pasteText;
                        var inputLength = pasteText.Length;
                        var selectionStart = txtBox.SelectionStart;
                        var selectionLength = txtBox.SelectionLength;

                        if (!string.IsNullOrEmpty(txtBox.Text))
                        {
                            inputText = txtBox.Text.Remove(selectionStart, selectionLength);
                            inputText = inputText.Insert(selectionStart, pasteText);
                        }

                        if (inputText.Contains("%"))
                        {
                            inputText = inputText.Remove(inputText.IndexOf('%'), 1);
                        }

                        // this operation's needed for the proper
                        // TryParse functionality method
                        if (_currentCultureSeparator == ',')
                        {
                            inputText = inputText.Replace(Dot, Comma);
                        }

                        decimal result;
                        if (!decimal.TryParse(inputText, NumberStyles.Any, _currentCulture, out result))
                        {
                            isPastedTextValid = false;
                        }
                        else
                        {
                            // ToDecimal must always have
                            // separator point as a dot
                            if (inputText.Contains(Comma))
                            {
                                inputText = inputText.Replace(Comma, Dot);
                            }
                            var tmp = Convert.ToDecimal(inputText, CultureInfo.InvariantCulture);
                            if (!IsValidWithPrecision(inputText))
                            {
                                isPastedTextValid = false;
                            }
                            else
                            {
                                txtBox.Text = tmp.ToString();
                                txtBox.CaretIndex = selectionStart + inputLength;
                                Value = tmp;
                                isPastedTextValid = false;
                            }
                        }
                    }
                }

                txtBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();

                if (!isPastedTextValid)
                    e.CancelCommand();
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TxtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var txtBox = (TextBox)sender;
                if (txtBox != null)
                {
                    var str = txtBox.Text;
                    if (string.IsNullOrEmpty(str))
                    {
                        this.Value = 0;
                        txtBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                    }
                    else
                    {
                        if (str.Equals("N/A"))
                            return;

                        if (str.Contains("%"))
                            str.Remove(str.IndexOf('%'), 1);

                        decimal value;
                        if (!decimal.TryParse(str, NumberStyles.Any, _currentCulture, out value))
                        {
                            SetupDefaultValue();
                        }
                        else
                        {
                            ValidateLostFocusValue(str);
                        }
                    }
                }
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TxtBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (e.Delta > 0)
                {
                    IncreaseStepValue();
                }
                else if (e.Delta < 0)
                {
                    DecreaseStepValue();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void TxtBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.OemPlus)
            {
                e.Handled = true;
            }

            if (Precision < 1 || IsPercentageModeOn)
            {
                if (e.Key == Key.OemComma || e.Key == Key.OemPeriod)
                {
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Up)
            {
                try
                {
                    IncreaseStepValue();
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
                    DecreaseStepValue();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (e.Key == Key.OemMinus && Minimum >= 0)
            {
                e.Handled = true;
            }

            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                var str = txtBox.Text;

                // check if user has tried to delete after % sign.
                // If yes, then it's not allowed. Checking's based on
                // Current caret index. If it's not equal to Length
                // of a txtbox text then allow removing.
                if (IsPercentageModeOn && str.IndexOf('%') == str.Length - 1 && txtBox.CaretIndex == txtBox.Text.Length)
                {
                    e.Handled = true;
                }
            }
        }

        private void TxtBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var txtBox = (TextBox)sender;
            if (txtBox != null)
            {
                bool percentMode = false;

                // getting new text input argument 
                // and adding it to the current txtbox text
                // property depending on caret index.
                var caretIndex = txtBox.CaretIndex;
                var str = txtBox.Text.Insert(caretIndex, e.Text);

                // check if text input argument is entered
                // on already mouse selected piece of text. If yes,
                // then remove selected text and add new input.
                str = ValidateMouseSelectedText(str, e.Text);

                // check if input contains any of the
                // N/A characters and remove it if any.
                str = RemoveNA(str);

                // check if user has entered anything after 
                // percent, if yes then remove last char.
                if (str.Contains("%") && str.IndexOf('%') != str.Length - 1)
                {
                    str = RemoveLast(str);
                    percentMode = true;
                }

                // simple regex to valid if entered text input
                // is a number or not. Dots, Commas & Minuses
                // are allowed in this particular regex.
                if (IsValid(e.Text))
                {
                    e.Handled = true;
                    return;
                }

                // check if switched comma and dot are 
                // in right order. The key is to prevent
                // situation like "1.," in code.
                if (!IsCommaQueueInRightOrder(str))
                {
                    if (e.Text == Dot)
                    {
                        txtBox.Text = RemoveBySign(str, '.');
                        e.Handled = true;
                        return;
                    }
                    else if (e.Text == Comma)
                    {
                        txtBox.Text = RemoveBySign(str, ',');
                        e.Handled = true;
                        return;
                    }
                }

                // check for minuses duplicates.
                // and remove if necessary.
                if (HasDuplicate(str, '-'))
                {
                    if (str.Length != 2)
                    {
                        txtBox.Text = RemoveDuplicate(str, '-');
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        txtBox.Text = RemoveLast(str);
                        e.Handled = true;
                        return;
                    }
                }

                // check for dot duplicates.
                // and remove if necessary.
                if (HasDuplicate(str, '.'))
                {

                    if (str.Length != 2)
                    {
                        txtBox.Text = RemoveDuplicate(str, '.');
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        txtBox.Text = RemoveLast(str);
                        e.Handled = true;
                        return;
                    }
                }

                // check for comma duplicates.
                // and remove if necessary.
                if (HasDuplicate(str, ','))
                {
                    if (str[0] == ',')
                    {
                        txtBox.Text = str.Remove(0, 1);
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        txtBox.Text = RemoveDuplicate(str, ',');
                        e.Handled = true;
                        return;
                    }
                }

                // additional validation to prevent
                // values like '-6%b' etc.
                if (percentMode && str != Minus)
                {
                    Value = Convert.ToDecimal(str, CultureInfo.InvariantCulture);
                    percentMode = false;
                    e.Handled = true;
                }

                // Determine if current culture 
                // separator is dot or comma and then
                // switch those separators if needed.
                if (e.Text == Comma || e.Text == Dot)
                {
                    if (IsDotCurrentCulture() && e.Text == Comma)
                    {
                        if (str.Contains(Comma))
                        {
                            txtBox.Text = str.Replace(Comma, Dot);
                            txtBox.CaretIndex = txtBox.Text.Length;
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            txtBox.Text = txtBox.Text + Dot;
                            txtBox.CaretIndex = txtBox.Text.Length;
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (!IsDotCurrentCulture() && e.Text == Dot)
                    {
                        if (str.Contains(Dot))
                        {
                            txtBox.Text = str.Replace(Dot, Comma);
                            txtBox.CaretIndex = txtBox.Text.Length;
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            txtBox.Text = txtBox.Text + Comma;
                            txtBox.CaretIndex = txtBox.Text.Length;
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (str.Length > 1 && str[0] == ',' || str[0] == '.')
                    {
                        txtBox.Text = str.Remove(0, 1) + str[0].ToString();
                        txtBox.CaretIndex = str.Length;
                        e.Handled = true;
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(str) && str.Length == 2)
                {
                    if (str[1] == ',' || str[1] == '.')
                    {
                        txtBox.Text = str;
                        txtBox.CaretIndex = txtBox.Text.Length;
                        e.Handled = true;
                    }

                    if (str[1] == '0' && Precision < 1)
                    {
                        txtBox.Text = RemoveLast(str);
                        e.Handled = true;
                        return;
                    }

                    if (str[0] == ',' || str[0] == '.')
                    {
                        txtBox.Text = str.Insert(0, "0");
                        txtBox.CaretIndex = txtBox.Text.Length;
                        e.Handled = true;
                        return;
                    }
                }

                // prevent situation like letting user
                // entering a number which is not a number
                // like '0X'. If it's with decimal point
                // then it's alright to let the user 
                // input dot or comma.
                if (str.Length > 1 && str[0] == '0')
                {
                    if (str[1] != ',' && str[1] != '.')
                    {
                        txtBox.Text = RemoveLast(str);
                        e.Handled = true;
                    }
                    else if (Precision < 1 && e.Text == Minus)
                    {
                        txtBox.Text = str.Remove(0, 1);
                        e.Handled = true;
                    }
                }

                // special condition for checking
                // if user tried to entered a number
                // like '-0X' and to prevent it.
                if (str.Length == 3 && str[2] != ',' && str[2] != '.' && str[0] == '-' && str[1] == '0')
                {
                    txtBox.Text = RemoveLast(str);
                    e.Handled = true;
                }

                // check if minus is somewhere in
                // the middle like "6-6" integer. If so
                // then remove this particular minus.
                if (str.Contains(Minus) && str.IndexOf('-') != 0)
                {
                    str = RemoveBySign(str, '-');
                    txtBox.Text = str;
                    txtBox.CaretIndex = caretIndex;
                    e.Handled = true;
                }

                // check for precision correctness,
                // and forbbid adding more digits
                // then precision cap.
                if (!IsValidWithPrecision(str))
                {
                    txtBox.Text = RemoveLast(str);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var str = RemoveNA(txtBox.Text);
                if (!string.IsNullOrEmpty(str))
                {
                    if (str.Contains("%") && str.Length > 0)
                    {
                        str = RemoveLast(str);
                    }

                    if (str.Contains(Comma))
                    {
                        str = str.Replace(Comma, Dot);
                    }

                    if (str.Length >= 1 && str.IndexOf('.') != str.Length - 1)
                    {
                        if (!(str.Length == 1 && str[0] == '-'))
                        {
                            var tmp = Convert.ToDecimal(str, CultureInfo.InvariantCulture);
                            Value = tmp;
                            txtBox.CaretIndex = str.Length;
                            txtBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                // The conversion from string to decimal overflowed.
            }
            catch (FormatException)
            {
                // The string is not formatted as a decimal.
            }
        }

        private void UpArrow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IncreaseStepValue();
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
                DecreaseStepValue();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void NumericUpDownControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetupDefaultValue();
        }
        #endregion

        #region Methods 
        private void SetupDefaultValue()
        {
            try
            {
                this.Value = this.DefaultValue;
                txtBox.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SetupInvariantCulture()
        {
            try
            {
                _currentCultureSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                _currentCulture = CultureInfo.CurrentCulture;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool IsDotCurrentCulture()
        {
            if (!char.IsWhiteSpace(_currentCultureSeparator))
            {
                if (_currentCultureSeparator == '.')
                {
                    return true;
                }
                else if (_currentCultureSeparator == ',')
                {
                    return false;
                }
            }
            return false;
        }

        private void CheckMinimumMaximumCap(string str)
        {
            try
            {
                var tmp = Convert.ToDecimal(str, CultureInfo.InvariantCulture);
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
                    Value = tmp;
                }
            }
            catch (FormatException)
            {
                this.Value = this.DefaultValue;
            }
            catch (OverflowException)
            {
                this.Value = this.DefaultValue;
            }
            catch (Exception)
            {
                this.Value = this.DefaultValue;
            }
        }

        private void ValidateLostFocusValue(string str)
        {
            if (str.Contains(",") && str.IndexOf(',') == str.Length - 1)
            {
                this.Value = Decimal.Parse(RemoveLast(str));
            }
            else if (str.Contains(".") && str.IndexOf('.') == str.Length - 1)
            {
                this.Value = Decimal.Parse(RemoveLast(str));
            }
            else if (str.Length == 2 && str[0] == '-' && str[1] == '0')
            {
                this.Value = this.DefaultValue;
            }
            else if (str.Contains(","))
            {
                this.Value = Convert.ToDecimal(str.Replace(Comma, Dot), CultureInfo.InvariantCulture);
            }

            CheckMinimumMaximumCap(str);
        }

        private void IncreaseStepValue()
        {
            var tmp = Value;
            if (tmp >= Maximum)
            {
                this.Value = Maximum;
            }
            else
            {
                this.Value += StepValue;
            }
        }

        private void DecreaseStepValue()
        {
            var tmp = Value;
            if (tmp <= Minimum)
            {
                this.Value = Minimum;
            }
            else
            {
                this.Value -= StepValue;
            }
        }

        private string ValidateMouseSelectedText(string str, string arg)
        {
            var selectionStart = txtBox.SelectionStart;
            var selectionLength = txtBox.SelectionLength;

            if (selectionLength > 0)
            {
                str = txtBox.Text.Remove(selectionStart, selectionLength);
                return str.Insert(selectionStart, arg);
            }

            return str;
        }

        private string MoveCharToFirst(string text, string sign)
        {
            if (text.Contains(sign))
            {
                var indexOfSign = text.IndexOf(sign);
                var fullLength = text.Length - 1;
                return text.Remove(indexOfSign, 1).Insert(0, sign);
            }
            return text;
        }

        private string MoveLastToFirst(string text)
        {
            var last = text[text.Length - 1];
            return RemoveLast(text).Insert(0, last.ToString());
        }

        private string RemoveBySign(string text, char sign)
        {
            var indexOfSign = text.IndexOf(sign);
            return text.Remove(indexOfSign, 1);
        }

        private string RemoveLast(string text)
        {
            return text.Remove(text.Length - 1);
        }

        private string RemoveDuplicate(string text, char sign)
        {
            try
            {
                int i = 0;
                int signIndex = text.IndexOf(sign);
                int[] indexesofSign = new int[text.Length - 1];
                while (signIndex != -1)
                {
                    signIndex = text.IndexOf(sign, signIndex + 1);
                    indexesofSign[i] = signIndex;
                    i++;
                }
                return text.Remove(indexesofSign[0], 1);
            }
            catch (IndexOutOfRangeException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool IsMinusLast(string text)
        {
            if (text.Contains(Minus))
            {
                var indexOfMinus = text.IndexOf('-');
                var totalLength = text.Length - 1;

                if (indexOfMinus == totalLength)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsValidWithPrecision(string text)
        {
            if (IsDotCurrentCulture())
            {
                if (text.Contains(Dot))
                {
                    var numbersAfterDot = text.Length - text.IndexOf(Dot) - 1;
                    if (numbersAfterDot > Precision)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (text.Contains(Comma))
                {
                    var numbersAfterComma = text.Length - text.IndexOf(Comma) - 1;
                    if (numbersAfterComma > Precision)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsCommaQueueInRightOrder(string text)
        {
            if (text.Contains(Dot) && text.Contains(Comma))
            {
                return false;
            }
            else if (text.Contains(Minus) && text.Contains(Dot))
            {
                var minusIndex = text.IndexOf('-');
                var dotIndex = text.IndexOf('.');
                if (minusIndex == dotIndex - 1)
                {
                    return false;
                }
            }
            else if (text.Contains(Minus) && text.Contains(Comma))
            {
                var minusIndex = text.IndexOf('-');
                var commaIndex = text.IndexOf(',');
                if (minusIndex == commaIndex - 1)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsNumeric(string text)
        {
            // valid = ["123.12", "2", "56754", "92929292929292.12", "0.21", "3.1"]
            // invalid = ["12.1232", "2.23332", "e666.76"]
            return Regex.IsMatch(text, @"^[0-9]+(\.\,[0-9]{1,2})?$");
        }

        private bool IsNumericWithDot(string text)
        {
            return Regex.IsMatch(text, @"^[0-9]+(\.[0-9]{1,2})?$");
        }

        private bool IsValid(string text)
        {
            return Regex.IsMatch(text, @"[^0-9.,-]+");
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

        private string RemoveNA(string text)
        {
            var retValue = text;
            if (retValue.Contains("N"))
            {
                retValue = retValue.Remove(retValue.IndexOf('N'), 1);
            }

            if (retValue.Contains("/"))
            {
                retValue = retValue.Remove(retValue.IndexOf('/'), 1);
            }

            if (retValue.Contains("A"))
            {
                retValue = retValue.Remove(retValue.IndexOf('A'), 1);
            }
            return retValue;
        }

        /// <summary>
        /// Method created to Test dots and commas
        /// depending on current culture. In order to
        /// test it, use this function in constructor
        /// just before getting current separator.
        /// </summary>
        /// <param name="culture"></param>
        /// - polish format "pl-PL"
        /// - english format "en-GB"
        /// - american format "en-US"
        private void ChangeCurrentCulture(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
        }
        #endregion

        #region DependencyProperty Callbacks
        private static void NumericUpDownNAChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericUpDown = d as NumericUpDown;
            if (numericUpDown != null)
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
                        Source = this,
                        Path = new PropertyPath("Value"),
                        Mode = BindingMode.TwoWay,
                        Converter = converter
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
            var numericUpDown = d as NumericUpDown;
            if (numericUpDown != null)
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
                        Source = this,
                        Path = new PropertyPath("Value"),
                        Mode = BindingMode.TwoWay,
                        Converter = converter
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
        private char _currentCultureSeparator;
        private CultureInfo _currentCulture;

        public NotAvailableConverter()
        {
            SetupInvariantCulture();
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                if (_currentCultureSeparator == ',')
                {
                    if (retValue.Contains("."))
                    {
                        retValue = ((decimal)value).ToString().Replace(".", ",");
                    }
                }
                else if (_currentCultureSeparator == '.')
                {
                    if (retValue.Contains(","))
                    {
                        retValue = ((decimal)value).ToString().Replace(",", ".");
                    }
                }

                if (retValue == "-1")
                {
                    return "N/A";
                }
                else if (retValue == "-1.0")
                {
                    return "N/A";
                }
            }
            return ((decimal)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                if (_currentCultureSeparator == ',')
                {
                    if (retValue.Contains("."))
                    {
                        retValue.Replace(".", ",");
                    }
                }
                else if (_currentCultureSeparator == '.')
                {
                    if (retValue.Contains(","))
                    {
                        retValue.Replace(",", ".");
                    }
                }

                Regex regex = new Regex(@"[^0-9.,-]+");
                retValue = regex.Replace(retValue, string.Empty);
                if (retValue == "-")
                {
                    return retValue.Replace("-", "0");
                }
                return retValue;
            }
            return value;
        }

        private void SetupInvariantCulture()
        {
            try
            {
                _currentCultureSeparator = System.Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                _currentCulture = CultureInfo.CurrentCulture;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    [ValueConversion(typeof(decimal), typeof(string))]
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                return ((decimal)value).ToString() + "%";
            }
            return ((decimal)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                if (retValue.Contains("%"))
                {
                    return retValue.Remove(retValue.IndexOf('%'), 1);
                }
            }
            return value;
        }
    }

    [ValueConversion(typeof(decimal), typeof(string))]
    public class DecimalPointConverter : IValueConverter
    {
        private char _currentCultureSeparator;
        private CultureInfo _currentCulture;

        public DecimalPointConverter()
        {
            SetupInvariantCulture();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                if (_currentCultureSeparator == ',')
                {
                    if (retValue.Contains("."))
                    {
                        return ((decimal)value).ToString().Replace(".", ",");
                    }
                }
                else if (_currentCultureSeparator == '.')
                {
                    if (retValue.Contains(","))
                    {
                        return ((decimal)value).ToString().Replace(",", ".");
                    }
                }
            }
            return ((decimal)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var retValue = value.ToString();
            if (!string.IsNullOrEmpty(retValue))
            {
                if (_currentCultureSeparator == ',')
                {
                    if (retValue.Contains("."))
                    {
                        return retValue.Replace(".", ",");
                    }
                }
                else if (_currentCultureSeparator == '.')
                {
                    if (retValue.Contains(","))
                    {
                        return retValue.Replace(",", ".");
                    }
                }
            }
            return value;
        }

        private void SetupInvariantCulture()
        {
            try
            {
                _currentCultureSeparator = System.Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                _currentCulture = CultureInfo.CurrentCulture;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    #endregion
}
