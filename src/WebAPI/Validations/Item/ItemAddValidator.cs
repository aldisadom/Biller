﻿using Common.Validators;
using Contracts.Requests.Item;
using FluentValidation;
using FluentValidation.Validators;
using System.ComponentModel.DataAnnotations;

namespace WebAPI.Validations.Item;

/// <summary>
/// Item add validation
/// </summary>
public class ItemAddValidator : AbstractValidator<ItemAddRequest>
{    
    /// <summary>
    /// Validation
    /// </summary>
    public ItemAddValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Please specify customer Id");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please name of item");
        RuleFor(x => x.Price).NotEmpty().WithMessage("Please specify price of item")
            .GreaterThan(0).WithMessage("Price should be more than zero");
        RuleFor(x => x.Quantity).NotEmpty().WithMessage("Please specify quantity of item")
            .GreaterThanOrEqualTo(-1).WithMessage("Quantity can not be negative, except -1 quantity not used");
    }
}
