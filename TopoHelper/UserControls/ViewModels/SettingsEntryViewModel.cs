using System;
using System.Collections.Generic;

namespace TopoHelper.UserControls.ViewModels
{
    public class SettingsEntryViewModel : BaseViewModel
    {
        #region Private Fields

        private readonly object _originalValue = null;
        private string _name;
        private Type _type;
        private string _valueString;

        #endregion

        #region Public Constructors

        public SettingsEntryViewModel(Type propertyType, object originalValue)
        {
            _type = propertyType;
            _originalValue = originalValue;
            _valueString = originalValue.ToString();
            SetObjectValueFromString(_valueString);
        }

        #endregion

        #region Public Properties

        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();
        public bool HasErrors { get => Errors.Count > 0; }
        public string Name { get => _name; internal set => SetName(value); }
        public Type Type { get => _type; internal set => SetType(value); }
        protected object Value { get; private set; }

        public string ValueString {
            get {
                return _valueString;
            }
            set {
                _valueString = value;
                SetObjectValueFromString(_valueString);
                RaisePropertyChanged(nameof(Value));

                // Set Dirty State
                if (Value.Equals(_originalValue))
                { IsDirty = false; }
                else { IsDirty = true; }
            }
        }

        #endregion

        #region Internal Methods

        internal void SaveValueToObject(Properties.Settings properties)
        {
            var type = typeof(Properties.Settings);
            // var propertyList = type.GetProperties();
            if (Type == typeof(short))
                type.GetProperty(Name).SetValue(properties, Convert.ToInt16(Value));
            else if (Type == typeof(bool))
                type.GetProperty(Name).SetValue(properties, Convert.ToBoolean(Value));
            else if (Type == typeof(double))
                type.GetProperty(Name).SetValue(properties, Convert.ToDouble(Value));
            else if (Type == typeof(string))
                type.GetProperty(Name).SetValue(properties, Value.ToString());
            else
                throw new NotImplementedException("Unknown type encountered, type not yet implemented.");
        }

        #endregion

        #region Private Methods

        private void AddError(string propertyName, string errorMessage)
        {
            if (Errors.ContainsKey(propertyName))
                _ = Errors.Remove(propertyName);
            Errors.Add(propertyName, errorMessage);
            RaisePropertyChanged(nameof(HasErrors));
            RaisePropertyChanged(nameof(Errors));
        }

        private void RemoveError(string propertyName)
        {
            if (Errors.ContainsKey(propertyName))
            {
                Errors.Remove(propertyName);
                RaisePropertyChanged(nameof(HasErrors));
                RaisePropertyChanged(nameof(Errors));
            }
        }

        private void SetName(string value)
        {
            _name = value;
            RaisePropertyChanged(nameof(Name));
        }

        /// <summary>
        /// Here we test converting the input of the user to a value that can be
        /// accepted as further input in the application (AutoCAD, ...).
        /// </summary>
        /// <returns> The interpreted string. </returns>
        private void SetObjectValueFromString(string value)
        {
            if (Type == null) throw new InvalidOperationException("Type needs to be initialized and set from constructor before calling this function!");

            if (Type == typeof(bool))
            {
                if (bool.TryParse(value.ToString(), out bool result))
                {
                    RemoveError(nameof(Value));
                    Value = result;
                }
                else
                {
                    AddError(nameof(Value), $"Invalid value provided for type: {Type.FullName}");
                    Value = result;
                }
            }
            else if (Type == typeof(short))
            {
                if (short.TryParse(value.ToString(), out short result))
                {
                    RemoveError(nameof(Value));
                    Value = result;
                }
                else
                {
                    AddError(nameof(Value), $"Invalid value provided for type: {Type.FullName}");
                    Value = result;
                }
            }
            else if (Type == typeof(double))
            {
                if (double.TryParse(value.ToString(), out double result))
                {
                    RemoveError(nameof(Value));
                    Value = result;
                }
                else
                {
                    AddError(nameof(Value), $"Invalid value provided for type: {Type.FullName}");
                    Value = string.Empty;
                }
            }
            else if (Type == typeof(string))
            {
                RemoveError(nameof(Value));
                Value = value.ToString();
            }
            else
                AddError(nameof(Value), $"Invalid value provided for type: {Type.FullName}");
        }

        private void SetType(Type value)
        {
            _type = value;
            RaisePropertyChanged(nameof(Type));
        }

        #endregion
    }
}