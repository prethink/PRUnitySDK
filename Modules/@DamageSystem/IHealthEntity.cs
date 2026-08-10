using UnityEngine;

public interface IHealthEntity
{
    /// <summary>
    /// ������������ ���������� ��������.
    /// </summary>
    public float MaxHealth { get; }

    /// <summary>
    /// ������� ��������.
    /// </summary>
    public float Health { get; }

    /// <summary>
    /// ������� ������.
    /// </summary>
    public EntityBase Entity { get; }

    /// <summary>
    /// ������� ������.
    /// </summary>
    public GameObject GameObject { get; }

    /// <summary>
    /// ������
    /// </summary>
    public IEntity Killer { get; }

    /// <summary>
    /// ����� ��������.
    /// </summary>
    public bool Kill();

    /// <summary>
    /// ����� ��������..
    /// </summary>
    /// <param name="killer">������.</param>
    public bool IsKill(IEntity killer);

    /// <summary>
    /// ������� entity.
    /// </summary>
    public void Revive();


    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="transform">transform.</param>
    public void Revive(Transform transform);

    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="position">�������.</param>
    public void Revive(Vector3 position);


    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="health">���������� ������ ��� ���������.</param>
    public void Revive(float health);

    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="health">���������� ������ ��� ���������.</param>
    /// <param name="transform">transform.</param>
    public void Revive(float health, Transform transform);

    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="health">���������� ������ ��� ���������.</param>
    /// <param name="position">�������.</param>
    public void Revive(float health, Vector3 position);

    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="reviver">��� ��������.</param>
    /// <param name="health">���������� ������ ��� ���������.</param>
    /// <param name="transform">transform.</param>
    public void Revive(IEntity reviver, float health, Transform transform);

    /// <summary>
    /// ������� entity.
    /// </summary>
    /// <param name="reviver">��� ��������.</param>
    /// <param name="health">���������� ������ ��� ���������.</param>
    /// <param name="position">�������.</param>
    /// <param name="rotation">�������.</param>
    public void Revive(IEntity reviver, float health, Vector3 position, Quaternion rotation);

    /// <summary>
    /// ���������� ��������.
    /// </summary>
    /// <param name="spawnPosition">����� ������.</param>
    public void Spawn(Vector3 spawnPosition);

    /// <summary>
    /// �������, ��� �������� ����.
    /// </summary>
    /// <returns>True - ����, False - ������.</returns>
    public bool IsAlive();
}
