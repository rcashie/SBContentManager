
namespace AssetBasedContentManager
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ContentLoadingGame game = new ContentLoadingGame())
            {
                game.Run();
            }
        }
    }
#endif
}

