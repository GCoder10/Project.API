using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project.API.Models;

namespace Project.API.Data
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly DataContext _context;
        public ApplicationRepository(DataContext context)
        {
            _context = context;
        }


        public async Task<List<User>> GetUsers()
        {
            var users = await _context.Users.Include(p => p.Photos).ToListAsync();

            return users;
        }


        public async Task<List<Instance>> GetInstances()
        {
            var instances = await _context.Instances.ToListAsync();

            return instances;
        }

        public async Task<List<Bill>> GetBills()
        {
            var bills = await _context.Bills.ToListAsync();

            return bills;
        }


        public async Task<List<Instance>> GetInstancesForWorker(int id)
        {
            var instancesForWorker = await _context.Instances.Where(u => u.UserId == id).ToListAsync();

            return instancesForWorker;
        }


        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }
    }
}
