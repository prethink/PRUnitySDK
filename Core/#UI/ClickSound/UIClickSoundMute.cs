using UnityEngine;

/// <summary>
/// Метка: общий щелчок (<see cref="UIClickSoundPlayer"/>) здесь не звучит.
/// </summary>
/// <remarks>
/// Ставится на элемент или его предка. Нужна кнопкам со своим звуком нажатия — иначе звучали бы
/// оба — и элементам, которые нажимают часто и щелчок на которых мешает.
/// </remarks>
public sealed class UIClickSoundMute : MonoBehaviour
{
}
