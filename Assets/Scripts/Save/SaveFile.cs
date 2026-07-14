using System;

[Serializable]
public class SaveFile
{
    public PersistentData persistent = new();
    public RunData run = new();
}