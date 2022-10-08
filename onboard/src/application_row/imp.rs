use gtk4::glib;
use gtk4::prelude::*;
use gtk4::subclass::prelude::*;

use gtk4::CompositeTemplate;

#[derive(Debug, Default, CompositeTemplate)]
#[template(file = "application_row.ui")]
pub struct ApplicationRow {
    #[template_child]
    pub name: TemplateChild<gtk4::Label>,
    #[template_child]
    pub description: TemplateChild<gtk4::Label>,
    #[template_child]
    pub image: TemplateChild<gtk4::Image>,
}

#[glib::object_subclass]
impl ObjectSubclass for ApplicationRow {
    const NAME: &'static str = "ApplicationRow";
    type Type = super::ApplicationRow;
    type ParentType = gtk4::Box;

    fn class_init(klass: &mut Self::Class) {
        klass.bind_template();
    }

    fn instance_init(obj: &glib::subclass::InitializingObject<Self>) {
        obj.init_template();
    }
}

impl ObjectImpl for ApplicationRow {}
impl WidgetImpl for ApplicationRow {}
impl BoxImpl for ApplicationRow {}
