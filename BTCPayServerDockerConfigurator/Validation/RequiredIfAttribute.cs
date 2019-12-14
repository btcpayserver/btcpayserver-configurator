using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Validation
{
    //from https://stackoverflow.com/questions/52321148/conditional-validation-in-mvc-net-core-requiredif
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly bool _notDesiredValue;
        private String PropertyName { get; set; }
        private Object DesiredValue { get; set; }
        

        public RequiredIfAttribute(String propertyName, Object desiredvalue, String errormessage, bool notDesiredValue = false)
        {
            _notDesiredValue = notDesiredValue;
            this.PropertyName = propertyName;
            this.DesiredValue = desiredvalue;
            this.ErrorMessage = errormessage;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            Object instance = context.ObjectInstance;
            Type type = instance.GetType();
            Object proprtyvalue = type.GetProperty(PropertyName).GetValue(instance, null);

            if (value != null)
            {
                return ValidationResult.Success;
            }else if (_notDesiredValue && proprtyvalue.ToString() != DesiredValue.ToString())
            {
                return new ValidationResult(ErrorMessage);
            }else if (!_notDesiredValue && proprtyvalue.ToString() == DesiredValue.ToString())
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
           
        }
    }
}