﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Mvc;
using Web.Models.Estado;
using Web.Models.Pais;

namespace Web.Controllers
{
    public class EstadoController : LoginExtention
    {
        // GET: Estado
        public ActionResult Index()
        {
            try
            {
                List<EstadoViewModel> minhaLista = getEstados();
                return View(minhaLista);
            }
            catch (HttpRequestExceptionEx ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }            
        }

        public class HttpRequestExceptionEx : Exception
        {
            public HttpRequestExceptionEx(string message) : base(message) { }
        }

        public List<EstadoViewModel> getEstados()
        {            
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("http://gestaoreceitaapi.somee.com/api/Estados");

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {
                    var stringResult = response.Result.Content.ReadAsStringAsync();

                    var estadoToList = JsonConvert.DeserializeObject<List<EstadoTO>>(stringResult.Result);
                    List<EstadoViewModel> estadoViewModelList = new List<EstadoViewModel>();

                    foreach (var estadoTO in estadoToList)
                    {
                        EstadoViewModel estadovm = new EstadoViewModel
                        {
                            id = estadoTO.id,
                            descricaoEstado = estadoTO.descricaoEstado,
                            idPais = estadoTO.idPais,
                            pais = (PaisViewModel)estadoTO.pais

                        };

                        estadoViewModelList.Add(estadovm);
                    }

                    return estadoViewModelList;
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro ao pegar dados de Estado: " + response.Result.ReasonPhrase);
                }
            }            
        }

        public ActionResult AdicionarEstado(EstadoViewModel novoEstado)
        {
            List<EstadoViewModel> minhaLista = getEstados();

            if (ModelState.IsValid)
            {
                bool estadoJaExiste = minhaLista.Any(p => p.descricaoEstado.Equals(novoEstado.descricaoEstado, StringComparison.OrdinalIgnoreCase));

                if (estadoJaExiste)
                {
                    ModelState.AddModelError("estadoExistente", "Este estado já existe.");
                    var erros = ModelState.Values.SelectMany(v => v.Errors).ToList();

                    return Json(new { success = false, erros });                    
                }

                List<PaisViewModel> listaPaises = this.getPaises();

                novoEstado.idPais = 0;

                foreach (var meuPais in listaPaises)
                {
                    if (meuPais.descricaoPais == novoEstado.descricaoPais)
                    {
                        novoEstado.idPais = meuPais.id;
                    }
                }

                int novoId = minhaLista.Max(max => max.id) + 1;

                this.CadastrarNovoEstado(novoEstado, novoId);

                if (!ModelState.IsValid)
                {                    
                    ModelState.AddModelError("error", "Erro ao cadastrar Estado.");
                    var erros = ModelState.Values.SelectMany(v => v.Errors).ToList();

                    return Json(new { success = false, erros });
                }
            }

            return PartialView("_CreateEstadoPartial", minhaLista);
        }

        private void CadastrarNovoEstado(EstadoViewModel novoEstado, int novoId)
        {
            using (var client = new HttpClient())
            {
                var formContentString = new StringContent(JsonConvert.SerializeObject(new { idPais = novoEstado.idPais, descricaoEstado = novoEstado.descricaoEstado, id = novoId }), Encoding.UTF8, "application/json");

                var response = client.PostAsync("http://gestaoreceitaapi.somee.com/api/Estados", formContentString);

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {                    
                    var stringResult = response.Result.Content.ReadAsStringAsync();
                    var objectJson = JsonConvert.DeserializeObject<EstadoTO>(stringResult.Result);
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro ao cadastrar novo Estado: " + response.Result.ReasonPhrase);
                }
            }
        }

        public ActionResult EditarEstado(EstadoViewModel estadoEditar)
        {
            var paises = getPaises();

            EstadoViewModel dadosEstado = getEstadoById(estadoEditar.id);

            EstadoViewModel estadoView = new EstadoViewModel()
            {
                id = dadosEstado.id,
                descricaoEstado = dadosEstado.descricaoEstado,
                idPais = dadosEstado.idPais,
                pais = dadosEstado.pais,
                listaPaises = paises
            };

            List<EstadoViewModel> minhaLista = getEstados();

            if (this.ModelState.IsValid)
            {
                bool estadoJaExiste = minhaLista.Any(p => p.descricaoEstado.Equals(estadoEditar.descricaoEstado, StringComparison.OrdinalIgnoreCase));

                if (estadoJaExiste)
                {
                    ModelState.AddModelError("estadoExistente", "Este estado já existe.");
                    var erros = ModelState.Values.SelectMany(v => v.Errors).ToList();

                    return Json(new { success = false, erros });
                }

                this.NovaEdicaoEstado(estadoEditar, dadosEstado);

                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("error", "Erro ao editar Estado");
                    var erros = ModelState.Values.SelectMany(v => v.Errors).ToList();

                    return Json(new { success = false, erros });
                }
            }

            return PartialView("_UpdateEstadoPartial", estadoView);
        }

        private void NovaEdicaoEstado(EstadoViewModel estadoEditar, EstadoViewModel dadosEstado)
        {
            using (var client = new HttpClient())
            {
                var formContentString = new StringContent(JsonConvert.SerializeObject(new { idPais = estadoEditar.idPais, descricaoEstado = estadoEditar.descricaoEstado, id = estadoEditar.id }), Encoding.UTF8, "application/json");

                var response = client.PutAsync(new Uri("http://gestaoreceitaapi.somee.com/api/Estados/" + dadosEstado.id), formContentString);

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {                    
                    var stringResult = response.Result.Content.ReadAsStringAsync();
                    var objectJson = JsonConvert.DeserializeObject<EstadoTO>(stringResult.Result);
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro ao editar novo Estado: " + response.Result.ReasonPhrase);
                }
            }
        }

        public JsonResult DeletarEstado(int Id)
        {
            var retorno = "";

            using (var client = new HttpClient())
            {
                var response = client.DeleteAsync("http://gestaoreceitaapi.somee.com/api/Estados/" + Id);

                response.Wait();

                retorno = "Estado deletado com sucesso";

                if (!response.Result.IsSuccessStatusCode)
                {
                    retorno = "Erro: " + response.Result.ReasonPhrase;
                }
            }
            return Json(new { mensagemRetorno = retorno });
        }

        //Partial Create
        public ActionResult getModalPaises()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("http://gestaoreceitaapi.somee.com/api/Pais");

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {
                    var stringResult = response.Result.Content.ReadAsStringAsync();

                    var paisToList = JsonConvert.DeserializeObject<List<PaisTO>>(stringResult.Result);
                    List<PaisViewModel> paisViewModelList = new List<PaisViewModel>();

                    foreach (var paisTo in paisToList)
                    {
                        PaisViewModel paisvm = new PaisViewModel
                        {
                            id = paisTo.id,
                            descricaoPais = paisTo.descricaoPais,
                        };

                        paisViewModelList.Add(paisvm);
                    }

                    return PartialView("_CreateEstadoPartial", paisViewModelList);
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro pegar dados de Modal Paises: " + response.Result.ReasonPhrase);
                }
            }            
        }

        //Partial Update
        public ActionResult getPaisesAndById(EstadoViewModel estadoViewModel)
        {
            var paises = getPaises();

            EstadoViewModel EstadosDados = getEstadoById(estadoViewModel.id);

            EstadoViewModel EstadoView = new EstadoViewModel()
            {
                id = EstadosDados.id,
                descricaoEstado = EstadosDados.descricaoEstado,
                idPais = EstadosDados.idPais,
                pais = EstadosDados.pais,
                listaPaises = paises
            };

            return PartialView("_UpdateEstadoPartial", EstadoView);

        }

        //CONSULTAS:
        private List<PaisViewModel> getPaises()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("http://gestaoreceitaapi.somee.com/api/Pais");

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {
                    var stringResult = response.Result.Content.ReadAsStringAsync();

                    var paisToList = JsonConvert.DeserializeObject<List<PaisTO>>(stringResult.Result);
                    List<PaisViewModel> paisViewModelList = new List<PaisViewModel>();

                    foreach (var paisTo in paisToList)
                    {
                        PaisViewModel paisvm = new PaisViewModel
                        {
                            id = paisTo.id,
                            descricaoPais = paisTo.descricaoPais,
                        };

                        paisViewModelList.Add(paisvm);
                    }

                    return paisViewModelList;
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro ao cadastrar novo Estado: " + response.Result.ReasonPhrase);
                }
            }            
        }

        public EstadoViewModel getEstadoById(int id)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("http://gestaoreceitaapi.somee.com/api/Estados/" + id);

                response.Wait();

                if (response.Result.IsSuccessStatusCode)
                {
                    var stringResult = response.Result.Content.ReadAsStringAsync();

                    var estadoTO = JsonConvert.DeserializeObject<EstadoTO>(stringResult.Result);

                    EstadoViewModel paisView = (EstadoViewModel)estadoTO;

                    return paisView;
                }
                else
                {
                    throw new HttpRequestExceptionEx("Erro ao pegar Estado By Id: " + response.Result.ReasonPhrase);
                }
            }            
        }     
    }
}