using System;

public class Session {

    public static Session CurrentSession { get; private set; }
    public static Action<string> OnMapChanged;
    public static Action<short, short> OnPositionChanged;

    public int AccountID;
    public INetworkEntity Entity { get; private set; }
    public string CurrentMap { get; private set; }
    public short CurrentXPosition { get; private set; }
    public short CurrentYPosition { get; private set; }

    public Session(INetworkEntity entity, int accountID) {
        if (entity.GetEntityType() != EntityType.PC) {
            throw new ArgumentException("Cannot start session with non player entity");
        }

        AccountID = accountID;
        this.Entity = entity;
    }

    public void SetCurrentMap(string mapname) {
        CurrentMap = mapname;
        OnMapChanged?.Invoke(mapname);
    }

    public void SetCurrentMapPosition(short xPos, short yPos)
    {
        CurrentXPosition = xPos;
        CurrentYPosition = yPos;
        OnPositionChanged?.Invoke(xPos, yPos);
    }

    public static void StartSession(Session session) {
        CurrentSession = session;
    }
}