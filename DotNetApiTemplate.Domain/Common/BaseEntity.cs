using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetApiTemplate.Domain.Common;

internal abstract class BaseEntity
{
    public Guid Id { get; set; }
}
