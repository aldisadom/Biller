﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Responses.User;

public class UserLoginResponse
{
    public string Token { get; set; } = string.Empty;
}
