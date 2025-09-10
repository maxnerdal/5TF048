using WebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{

    public class StudentController : Controller
    {
        static IList<StudentModel> studentList = new List<StudentModel>
            {
                new StudentModel() { Id = 1, Name = "Alice", Age = 20 },
                new StudentModel() { Id = 2, Name = "Bob", Age = 22 },
                new StudentModel() { Id = 3, Name = "Charlie", Age = 23 }
            };

        public IActionResult Students()
        {

            return View(studentList);
        }
    }
}