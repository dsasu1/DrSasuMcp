using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrSasuMcp.Tools.API.Models
{
    public enum ValidationType
    {
        StatusCode,
        Header,
        JsonPath,
        ResponseTime,
        BodyContains,
        BodyEquals,
        BodyRegex
    }
}

