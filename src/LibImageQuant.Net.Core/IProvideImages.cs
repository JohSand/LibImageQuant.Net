namespace LibImageQuant.Net.Core
{
    public interface IProvideImages
    {
        void ProvideImageRow(System.Span<Color> rowOut, int rowIndex);
    }
}