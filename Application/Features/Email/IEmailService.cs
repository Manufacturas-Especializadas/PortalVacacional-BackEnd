using Application.Features.Employee.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(IEnumerable<string> toEmails, string subject, string body);
    }
}
