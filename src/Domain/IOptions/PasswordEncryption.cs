using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.IOptions;

public class PasswordEncryption
{
    public string Salt { get; set; } = string.Empty;
}
