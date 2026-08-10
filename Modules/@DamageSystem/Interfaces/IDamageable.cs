using UnityEngine;

/// <summary>
/// ��������� ��� ��������, ������� ����� �������� ����.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ������� ���� ��������.
    /// </summary>
    /// <param name="attacker">��������, ��������� ����.</param>
    /// <param name="weapon">������, ������� ������ ���� (����� ���� null).</param>
    /// <param name="damage">���������� �� ����� (��������, ���, ���� � �.�.).</param>
    /// <returns>��������� ��������� ����� (��������, ��������� �������� �����, ��� �� ���� � �.�.).</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage);

    /// <summary>
    /// ������� ���� �������� � ����������� ����� ����.
    /// </summary>
    /// <param name="attacker">��������, ��������� ����.</param>
    /// <param name="weapon">������, ������� ������ ���� (����� ���� null).</param>
    /// <param name="damage">���������� �� ����� (��������, ���, ���� � �.�.).</param>
    /// <param name="point">����� � ������� �����������, ���� �������� ���� (��������, ��������� ����).</param>
    /// <returns>��������� ��������� �����.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Vector3 point);

    /// <summary>
    /// ������� ���� �������� � ����������� ���������.
    /// </summary>
    /// <param name="attacker">��������, ��������� ����.</param>
    /// <param name="weapon">������, ������� ������ ���� (����� ���� null).</param>
    /// <param name="damage">���������� �� ����� (��������, ���, ���� � �.�.).</param>
    /// <param name="collider">���������, �� �������� �������� ���� (��������, ����� ���� ��� ������ �����).</param>
    /// <returns>��������� ��������� �����.</returns>
    DamageResult TakeDamage(IEntity attacker, IWeapon weapon, IDamageProvider damage, Collider collider);
}
