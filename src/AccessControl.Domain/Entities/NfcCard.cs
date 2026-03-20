using System.ComponentModel.DataAnnotations;
using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

public class NfcCard
{
    public Guid Id { get; set; }

    [MaxLength(32)]
    public string Uid { get; set; } = string.Empty;

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public CardType CardType { get; set; } = CardType.Employee;

    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
}
