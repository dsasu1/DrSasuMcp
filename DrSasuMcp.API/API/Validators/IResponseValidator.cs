using DrSasuMcp.API.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.API.API.Validators
{
    public interface IResponseValidator
    {
        ValidationType SupportedType { get; }
        ValidationResult Validate(HttpResponseResult response, ValidationRule rule);
    }
}

