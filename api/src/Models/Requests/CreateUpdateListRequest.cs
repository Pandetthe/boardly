﻿using Boardly.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateUpdateListRequest
{
    [Required, MaxLength(40)]
    public string Title { get; init; } = null!;

    [Required]
    public Color Color { get; init; }
    
    public int? MaxWIP { get; init; }
}