using Contacts.Maui.Models;
using Contact = Contacts.Maui.Models.Contact;
namespace Contacts.Maui.Views;


[QueryProperty(nameof(ContactId),"Id")]
public partial class EditContactPage : ContentPage
{
	private Contact contact; 
	public EditContactPage()
	{
		InitializeComponent();
	}
	private void btnCancel_Clicked(object sender, EventArgs e)
	{
		Shell.Current.GoToAsync("..");
	}
	public string ContactId
	{
		set
		{
			contact = ContactRepository.GetContactById(int.Parse(value));
			if(contact != null)
			{
				contactCtrl.Name = contact.Name;
                contactCtrl.Address = contact.Address;
                contactCtrl.Phone = contact.Phone;
                contactCtrl.Email = contact.Email;
            }
			
		}
	}

	private void btnUpdate_Clicked(object sender, EventArgs e)
	{
		
		contact.Name = contactCtrl.Name;// je met a jour les champs du contact avec les nouvelles valeurs saisies par l'utilisateur
		contact.Address = contactCtrl.Address;
		contact.Phone = contactCtrl.Phone;
		contact.Email = contactCtrl.Email;

		ContactRepository.UpdateContact(contact.ContactId, contact);
		Shell.Current.GoToAsync("..");
	}

	private void contactCtrl_OnError(object sender, string e)
	{
		DisplayAlert("Error", e, "OK");
	}
}