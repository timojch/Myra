using Myra.MML;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Myra.Graphics2D.UI
{
	public interface IMenuItem: IItemWithId
	{
		[Browsable(false)]
		[XmlIgnore]
		Menu Menu { get; set; }

		[Browsable(false)]
		[XmlIgnore]
		int Index { get; set; }

		void Invoke();

		bool CloseAfterInvoke { get; }

		bool CanInteract { get; }
	}
}
