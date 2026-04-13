using System.ComponentModel.DataAnnotations;

namespace BTCPayServerDockerConfigurator.Validation;

//from https://stackoverflow.com/questions/52321148/conditional-validation-in-mvc-net-core-requiredif
public class RequiredIfAttribute : ValidationAttribute
{
    private readonly bool _notDesiredValue;
    private string PropertyName { get; set; }
    private object DesiredValue { get; set; }

    public RequiredIfAttribute(string propertyName, object desiredvalue, string errormessage, bool notDesiredValue = false)
    {
        _notDesiredValue = notDesiredValue;
        PropertyName = propertyName;
        DesiredValue = desiredvalue;
        ErrorMessage = errormessage;
    }

    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        object instance = context.ObjectInstance;
        var type = instance.GetType();
        object proprtyvalue = type.GetProperty(PropertyName).GetValue(instance, null);

        if (value != null)
        {
            return ValidationResult.Success;
        }
        else if (_notDesiredValue && proprtyvalue.ToString() != DesiredValue.ToString())
        {
            return new ValidationResult(ErrorMessage);
        }
        else if (!_notDesiredValue && proprtyvalue.ToString() == DesiredValue.ToString())
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
