using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;

namespace CURD
{
    public interface ICitizen
    {
        public ICollection<TbCitizen> GetAll();
        public TbCitizen GetByID(int id);
        public void Add(TbCitizen citizen);
        public void Update(TbCitizen citizen);
        public void Delete(int id);
    }
    public class OP_Citizen : ICitizen
    {
        Ai_Reports_Context context;
        public OP_Citizen(Ai_Reports_Context ctx)
        {
            this.context = ctx;
        }
        public ICollection<TbCitizen> GetAll()
        {
            try
            {
                return context.Citizen.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("حدث خطأ تقني أثناء جلب البيانات: " + ex.Message);
            }
        }
        public TbCitizen GetByID(int id)
        {
            try
            {
                var citizen = context.Citizen.Find(id);
                return citizen;
            }
            catch
            {
                return new TbCitizen();
            }
        }
        public void Add(TbCitizen citizen)
        {
            
            if (citizen == null) return;

            try
            {
                context.Citizen.Add(citizen);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
               
                throw new Exception("حدث خطأ أثناء إضافة المواطن: " + ex.Message);
            }
        }
        public void Update(TbCitizen citizen)
        {
            if (citizen == null) return;

            try
            {
                context.Entry(citizen).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception("حدث خطأ أثناء تحديث بيانات المواطن: " + ex.Message);
            }
        }
        public void Delete(int id)
        {
            try
            {
                var citizen = GetByID(id);
                if (citizen != null)
                {
                    context.Citizen.Remove(citizen);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("لا يمكن حذف السجل، قد يكون مرتبطاً ببيانات أخرى أو حدث خطأ فني: " + ex.Message);
            }
        }
    }
}
