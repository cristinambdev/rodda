import io
import unittest
from contextlib import redirect_stdout
from decimal import Decimal
from unittest.mock import patch

import calculadora


class CalculadoraTests(unittest.TestCase):
    def ejecutar_opcion(self, opcion, entradas=()):
        historial = []
        with patch("builtins.input", side_effect=list(entradas)):
            salida = io.StringIO()
            with redirect_stdout(salida):
                calculadora.calcular(opcion, historial)
        return historial, salida.getvalue()

    def test_suma_guarda_el_resultado_en_el_historial(self):
        historial, salida = self.ejecutar_opcion("1", ["2", "3"])

        self.assertEqual(historial, ["2 + 3 = 5"])
        self.assertIn("Resultado: 5", salida)

    def test_resta_multiplicacion_y_division(self):
        historial = []

        with patch("builtins.input", side_effect=["10", "4"]):
            calculadora.calcular("2", historial)
        with patch("builtins.input", side_effect=["2.5", "4"]):
            calculadora.calcular("3", historial)
        with patch("builtins.input", side_effect=["9", "2"]):
            calculadora.calcular("4", historial)

        self.assertEqual(
            historial,
            ["10 - 4 = 6", "2.5 * 4 = 10.0", "9 / 2 = 4.5"],
        )

    def test_division_por_cero_no_modifica_el_historial(self):
        historial, salida = self.ejecutar_opcion("4", ["8", "0"])

        self.assertEqual(historial, [])
        self.assertIn("No se puede dividir entre cero", salida)

    def test_raiz_cuadrada(self):
        historial, salida = self.ejecutar_opcion("5", ["25"])

        self.assertEqual(historial, ["sqrt(25) = 5.0"])
        self.assertIn("Resultado: 5.0", salida)

    def test_raiz_de_numero_negativo_no_modifica_el_historial(self):
        historial, salida = self.ejecutar_opcion("5", ["-1"])

        self.assertEqual(historial, [])
        self.assertIn("No se puede calcular la raiz", salida)

    def test_reintenta_hasta_recibir_un_numero_valido(self):
        with patch("builtins.input", side_effect=["abc", "7"]):
            salida = io.StringIO()
            with redirect_stdout(salida):
                numero = calculadora.leer_numero("Numero: ")

        self.assertEqual(numero, Decimal("7"))
        self.assertIn("Entrada no valida", salida.getvalue())

    def test_historial_vacio_muestra_un_aviso(self):
        historial, salida = self.ejecutar_opcion("6")

        self.assertEqual(historial, [])
        self.assertIn("Todavia no hay operaciones", salida)

    def test_menu_permite_salir(self):
        with patch("builtins.input", side_effect=["7"]):
            salida = io.StringIO()
            with redirect_stdout(salida):
                calculadora.main()

        self.assertIn("CALCULADORA", salida.getvalue())
        self.assertIn("Hasta luego", salida.getvalue())


if __name__ == "__main__":
    unittest.main()
