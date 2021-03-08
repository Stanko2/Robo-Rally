using Map;
using Mirror;

public static class TileSerializers
{
    private const byte TILE = 0;
    private const byte HOLE = 1;
    private const byte CONVEYORBELT = 2;
    private const byte CHECKPOINT = 3;

    public static void WriteTile(this NetworkWriter writer, SerializedTile tile)
    {
        switch (tile)
        {
            case SerializedCheckpoint _:
                writer.WriteByte(CHECKPOINT);
                break;
            case SerializedConveyorBelt _:
                writer.WriteByte(CONVEYORBELT);
                break;
            default:
                writer.WriteByte(TILE);
                break;
        }
        tile.Write(writer);
    }

    public static SerializedTile ReadTile(this NetworkReader reader)
    {
        byte type = reader.ReadByte();
        switch (type)
        {
            case TILE:
                return new SerializedTile(reader);
            case CHECKPOINT:
                return new SerializedCheckpoint(reader);
            case CONVEYORBELT:
                return new SerializedConveyorBelt(reader);
            default:
                return null;
        }
    }
}