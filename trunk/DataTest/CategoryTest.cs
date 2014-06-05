using System;
using Data.ServiceRepository;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataTest
{
    using System.Collections.Generic;

    using Data.DomainEntity;
    using Data.Repository;

    using Microsoft.Practices.Unity;

   



    [TestClass]
    public class CategoryTest
    {


        #region Teat Master
        [TestMethod]
        public void TestMaster()
        {

            var service = new CategoryService();
            GetAllCategory();
            var category = new Category
            {
                CategoryId = 1,
                Description = "New Category",
                ImageName = "No Image",
                IsActive = true,
                Name = "Test CAtegory",
                ParentCategoryId = null
            };
            var recordupdated = 0;
            var id = this.Add(service, category, out recordupdated);


           var data1 =  this.GetService(service, id);
            data1.Description = "Update Action";
            this.UpdateService(service, data1, out recordupdated);

            data1.IsActive = false;
            Active(service, category);
            var data = this.GetService(service, id);
            Delete(service, data1, out recordupdated);

             var pa = new Dictionary<string, object>();
            pa.Add("CategoryId", 0);
            pa.Add("ParentCategoryId", 15);
            GetChieldService(service, pa);

        }

        [TestMethod]
        public T GetService<T, TId>(Repository<T, TId> service, TId id) where T:class
        {
            var result = service.Get(id);

            Assert.IsNotNull(result, "Get Success"); 
            return result;
        }

        [TestMethod]
        public void GetChieldService<T, TId>(Repository<T, TId> service, Dictionary<string, object> param) where T : class
        {
            var result = service.GetAllChilds(param);
            Assert.IsNotNull(result, "Get Success");
        }

        [TestMethod]
        public TId Add<T, TId>(Repository<T, TId> service, T entity, out int recordupdated) where T : class
        {
            var id = service.Add(entity, out recordupdated);
            Assert.IsNotNull(id, "id");
            Assert.IsNotNull(recordupdated, "record updated");
            return id;
        }

        [TestMethod]
        public void UpdateService<T, TId>(Repository<T, TId> service, T entity, out int recordupdated) where T : class
        {
            service.Update(entity, out recordupdated);
            Assert.IsNotNull(recordupdated, "record updated");
        }

        [TestMethod]
        public void Active<T, TId>(Repository<T, TId> service, T entity) where T : class
        {
            service.ActiveDeactive(entity);
        }

        [TestMethod]
        public void Delete<T, TId>(Repository<T, TId> service, T entity, out int recordupdated) where T : class
        {
            service.Remove(entity, out recordupdated);
            Assert.IsNotNull(recordupdated, "record updated");

        }

        #endregion


        [TestMethod]
        public void GetAllCategory()
        {
            var service = new CategoryService();
            var getAll = service.GetAll();
           

        }

        [TestMethod]
        public void Get(object id)
        {
            var service = new CategoryService();
            var result = service.Get((long)id);

        }

        [TestMethod]
        public void GetChield()
        {
            var service = new CategoryService();
            var pa = new Dictionary<string, object>();
            pa.Add("CategoryId", 0);
            pa.Add("ParentCategoryId", 15);
            var result = service.GetAllChilds(pa);
        }

        [TestMethod]
        public object Add()
        {
            var service = new CategoryService();
           var id = service.Add(
                new Category
                    {
                        CategoryId = 1,
                        Description = "New Category",
                        ImageName = "No Image",
                        IsActive = true,
                        Name = "Test CAtegory",
                        ParentCategoryId = null
                    });
            return id;
            Assert.IsNotNull(id, "id");

        }

        [TestMethod]
        public void Update()
        {
            var service = new CategoryService();
            service.Update(
                new Category
                {
                    CategoryId = 10123,
                    Description = "Update New Category",
                    ImageName = "Update Image Update",
                    IsActive = true,
                    Name = "Update Test CAtegory",
                    ParentCategoryId = null
                });
        }

        [TestMethod]
        public void Active()
        {
            var service = new CategoryService();
            service.ActiveDeactive(
                new Category
                {
                    CategoryId = 10123,
                    Description = "New Category",
                    ImageName = "No Image",
                    IsActive = true,
                    Name = "Test CAtegory",
                    ParentCategoryId = null
                });
        }

        [TestMethod]
        public void Delete()
        {
            var service = new CategoryService();
            service.Remove(
                new Category
                {
                    CategoryId = 10123,
                    Description = "New Category",
                    ImageName = "No Image",
                    IsActive = true,
                    Name = "Test CAtegory",
                    ParentCategoryId = null
                });
        }


    }
}
