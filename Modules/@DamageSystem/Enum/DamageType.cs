using System;

[Flags]
public enum DamageType
{
    /// <summary>
    /// ����� ����.
    /// </summary>
    Generic = 0,
    /// <summary>
    /// ���� �� �������.
    /// </summary>
    Fall = 1 << 0, 
    /// <summary>
    /// ���� �� ����.
    /// </summary>
    Bullet = 1 << 1,
    /// <summary>
    /// ���� �� ����.
    /// </summary>
    Fire = 1 << 2,
    /// <summary>
    /// ���� �� ������
    /// </summary>
    Ice = 1 << 3,
    /// <summary>
    /// ������������� ����
    /// </summary>
    Electric = 1 << 4,
    /// <summary>
    /// �������� ����.
    /// </summary>
    Poison = 1 << 5,
    /// <summary>
    /// ��������.
    /// </summary>
    Radiation = 1 << 6,
    /// <summary>
    /// ���� �� ������.
    /// </summary>
    Explosion = 1 << 7,
    /// <summary>
    /// �������.
    /// </summary>
    Acid = 1 << 8,
    /// <summary>
    /// ��������������� ����.
    /// </summary>
    Mental = 1 << 9,
    /// <summary>
    /// ���� �� �������.
    /// </summary>
    AreaOfEffect = 1 << 10, 
    /// <summary>
    /// ����������� ����.
    /// </summary>
    Critical = 1 << 11,
    /// <summary>
    /// ��������� ������������� ����.
    /// </summary>
    TimeBased = 1 << 12,
}