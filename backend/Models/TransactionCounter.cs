// <copyright file="TransactionCounter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Models;

using System;

public partial class TransactionCounter
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string CounterKey { get; set; } = null!;

    public int LastSequence { get; set; }
}
