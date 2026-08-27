from decimal import Decimal, InvalidOperation
from math import sqrt


def leer_numero(mensaje):
    while True:
        entrada = input(mensaje).strip().replace(",", ".")
        try:
            return Decimal(entrada)
        except InvalidOperation:
            print("Entrada no valida. Escribe un numero, por ejemplo 12.5.")


def mostrar_resultado(operacion, resultado, historial):
    texto = f"{operacion} = {resultado}"
    historial.append(texto)
    print(f"\nResultado: {resultado}\n")


def calcular(opcion, historial):
    if opcion in {"1", "2", "3", "4"}:
        primer_numero = leer_numero("Primer numero: ")
        segundo_numero = leer_numero("Segundo numero: ")

        if opcion == "1":
            resultado = primer_numero + segundo_numero
            operacion = f"{primer_numero} + {segundo_numero}"
        elif opcion == "2":
            resultado = primer_numero - segundo_numero
            operacion = f"{primer_numero} - {segundo_numero}"
        elif opcion == "3":
            resultado = primer_numero * segundo_numero
            operacion = f"{primer_numero} * {segundo_numero}"
        else:
            if segundo_numero == 0:
                print("No se puede dividir entre cero.\n")
                return
            resultado = primer_numero / segundo_numero
            operacion = f"{primer_numero} / {segundo_numero}"

        mostrar_resultado(operacion, resultado, historial)
        return

    if opcion == "5":
        numero = leer_numero("Numero: ")
        if numero < 0:
            print("No se puede calcular la raiz de un numero negativo.\n")
            return
        resultado = Decimal(str(sqrt(float(numero))))
        mostrar_resultado(f"sqrt({numero})", resultado, historial)
        return

    if opcion == "6":
        if not historial:
            print("Todavia no hay operaciones.\n")
            return
        print("\nHistorial:")
        for indice, operacion in enumerate(historial, start=1):
            print(f"{indice}. {operacion}")
        print()
        return

    print("Opcion no valida. Elige una opcion del 1 al 7.\n")


def mostrar_menu():
    print()
    print("+----------------------------------+")
    print("|          CALCULADORA             |")
    print("+----------------------------------+")
    print("| 1. Sumar                         |")
    print("| 2. Restar                        |")
    print("| 3. Multiplicar                   |")
    print("| 4. Dividir                       |")
    print("| 5. Raiz cuadrada                 |")
    print("| 6. Ver historial                 |")
    print("| 7. Salir                         |")
    print("+----------------------------------+")


def main():
    historial = []
    while True:
        mostrar_menu()
        opcion = input("Selecciona una opcion: ").strip()
        if opcion == "7":
            print("Hasta luego.")
            break
        calcular(opcion, historial)


if __name__ == "__main__":
    main()
