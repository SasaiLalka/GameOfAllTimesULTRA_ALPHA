namespace MagiCave.Interfaces
{
    public interface IItem : IDrawable
    {
        void Use(IActor actor);
    }
}
