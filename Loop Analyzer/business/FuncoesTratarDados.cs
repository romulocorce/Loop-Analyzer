﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    public class FuncoesTratarDados
    {
        private int _codSequencia = 0;
       
        public FuncoesTratarDados()
        {
            _codSequencia = 0;
        }
        CultureInfo provider = new CultureInfo("pt-BR");

        public async Task<int> RetornaUltimoCodigoSequencial()
        {
            try
            {
                return _codSequencia;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }   

        public async Task<DateTime?> ValidaERetornaData(string dataString, string format)
        {
            try
            {
                if (string.IsNullOrEmpty(dataString?.Trim()))
                    return null;

                try
                {
                    DateTime retorno;

                    retorno = DateTime.ParseExact(dataString, format, provider);
                    return retorno;

                }
                catch (FormatException fx)
                {
                    throw new Exception("Formado Inadequado: " + dataString + " para o formato: " + format);
                }
                catch (CultureNotFoundException e)
                {
                    throw e; // Given Culture is not supported culture
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> AjustaTamanhoStringT(string text, int tamanho, string retornoDafalut = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(text?.Trim()))
                    return text.Trim().Length > tamanho ? text.ToUpper().Trim().Substring(0, tamanho) : text.ToUpper().Trim();

                else
                    return retornoDafalut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<string> SomenteNumeros(string Texto)
        {
            var numeros = "";

            try
            {
                if (!String.IsNullOrEmpty(Texto))
                {
                    for (var i = 0; i < Texto.Length; i++)
                    {
                        if (char.IsNumber(Texto, i))
                        {
                            numeros += Texto.Substring(i, 1);
                        }
                    }

                    return numeros;
                }
                return numeros;
            }
            catch (Exception)
            {
                return numeros;
            }
        }

        public async Task<string> LimpaAcento(string text)
        {
            string withDiacritics = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
            string withoutDiacritics = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";
            for (int i = 0; i < withDiacritics.Length; i++)
            {
                text = text.Replace(withDiacritics[i].ToString(), withoutDiacritics[i].ToString());
            }
            return text;
        }

        public async Task<string> ArrumaCnpjCpf(string pCnpjCpf, string tipo)
        {
            try
            {
                var result = "";

                pCnpjCpf = await SomenteNumeros(pCnpjCpf);

                if (!string.IsNullOrEmpty(pCnpjCpf))
                {
                    if (pCnpjCpf.Length == 11)
                    {
                        result = Convert.ToUInt64(pCnpjCpf).ToString(@"000\.000\.000\-00");
                    }
                    else if (pCnpjCpf.Length == 14)
                    {
                        result = Convert.ToUInt64(pCnpjCpf).ToString(@"00\.000\.000\/0000\-00");
                    }
                    else if (tipo == "CPF" && pCnpjCpf.Length <= 11)
                    {
                        result = Convert.ToUInt64(pCnpjCpf).ToString(@"000\.000\.000\-00");
                    }
                    else if (tipo == "CNPJ" && pCnpjCpf.Length <= 14)
                    {
                        result = Convert.ToUInt64(pCnpjCpf).ToString(@"00\.000\.000\/0000\-00");
                    }
                    else
                    {
                        result = pCnpjCpf;
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Invalido:", msg);
            }

        }

        public async Task<string> ArrumaCEP(string cep)
        {
            try
            {
                string retorno = "";
                if (!string.IsNullOrEmpty(cep))
                {
                    cep = await SomenteNumeros(cep);
                    retorno = cep.Length <= 8 ? cep : cep.Substring(0, 8);
                    return retorno;
                }
                return retorno;

            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<string> ConverteNome(string nome, string sobrenome, string codigoold)
        {
            try
            {
                string nomeRetorno = nome;

                if (string.IsNullOrEmpty(sobrenome) && !string.IsNullOrEmpty(nome))
                {
                    string[] cortarSobrenome = nome.Split(' ');
                    string nomeMontagem = "";
                    for (int i = 0; i < cortarSobrenome.Count(); i++)
                    {
                        if (i < cortarSobrenome.Count() - 1)
                            nomeMontagem += " " + cortarSobrenome[i];
                    }
                    nomeRetorno = nomeMontagem.Length >= 1 ? nomeMontagem : nome;
                }
                return nomeRetorno?.ToUpper().Trim().Length > 50 ? nomeRetorno?.ToUpper().Trim().Substring(0, 50) : nomeRetorno?.ToUpper().Trim();
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message + codigoold;
                throw new ArgumentException("Dado Invalido:", msg);
            }
        }

        public async Task<string> ConverteSobrenome(string nome, string sobrenome)
        {
            try
            {
                string sobrenomeRetorno = sobrenome?.ToUpper().Trim(); ;

                if (string.IsNullOrEmpty(sobrenomeRetorno) && !string.IsNullOrEmpty(nome))
                {
                    string[] cortarSobrenome = nome.ToUpper().Trim().Split(' ');

                    if (cortarSobrenome.Count() > 1)
                        sobrenomeRetorno = cortarSobrenome[cortarSobrenome.Count() - 1];
                }

                if (sobrenomeRetorno?.Length > 40)
                    sobrenomeRetorno = sobrenomeRetorno?.Substring(0, 40);

                return sobrenomeRetorno;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Invalido:", msg);
            }

        }

        public async Task<string> validaEmail(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                if (new EmailAddressAttribute().IsValid(email))
                {
                    return email;
                }
                else return "";
            }
            return "";
        }

        public async Task<int?> validaSexo(int? sexo)
        {
            if (sexo != 1 && sexo != 2)
            {
                sexo = 0;
            }
            return sexo;
        }
    }
}
