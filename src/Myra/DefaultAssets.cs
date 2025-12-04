using Myra.Graphics2D.UI.Styles;
using AssetManagementBase;
using System;

namespace Myra
{
	public static class DefaultAssets
	{
		private static AssetManager _assetManager;

		private static AssetManager AssetManager
		{
			get
			{
				if (_assetManager == null)
				{
					_assetManager = AssetManager.CreateResourceAssetManager(typeof(DefaultAssets).Assembly, "Resources.");
				}

				return _assetManager;
			}
		}

		[Obsolete("Use DefaultStylesheet")]
		public static Stylesheet UIStylesheet => DefaultStylesheet;

        public static Stylesheet DefaultStylesheet
        {
            get
            {
                if (field != null)
                {
                    return field;
                }

                field = AssetManager.LoadStylesheet("default_ui_skin.xmms");
                return field ;
            }

			private set;
        }

        public static Stylesheet DefaultClassicStylesheet2X
        {
            get
            {
                if (field != null)
                {
                    return field;
                }

                field = AssetManager.LoadStylesheet("default_ui_skin_2x.xmms");
                return field;
            }

            private set;
        }

		internal static void Dispose()
		{
            DefaultStylesheet = null;
            DefaultClassicStylesheet2X = null;

			if (_assetManager != null)
			{
				_assetManager.Cache.Clear();
				_assetManager = null;
			}
		}
	}
}