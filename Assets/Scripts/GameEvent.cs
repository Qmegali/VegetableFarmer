using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Game Event", menuName = "Game Event", order = 51)]
public class GameEvent : ScriptableObject
{
    HashSet<GameEventListener> _listeners = new HashSet<GameEventListener>();

    public void Invoke()
    {
        foreach (var gameEventListener in _listeners)
        {
            gameEventListener.RaiseEvent();
        }
    }

    public void Register(GameEventListener gameEventListener) => _listeners.Add(gameEventListener);

    public void Deregister(GameEventListener gameEventListener) => _listeners.Remove(gameEventListener);
}
