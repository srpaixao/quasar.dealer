$(function () {
    'use strict';

    if ($('.user-error').find('span').html().trim() != '') {
        $('.form-group.first').addClass('invalid-input');
    }

    $("#Usuario").on('blur', function () {
        $('.form-group.first').removeClass('invalid-input');
        $('.user-error').hide();
    })

    if ($('.pwd-error').find('span').html().trim() != '') {
        $('.form-group.last').addClass('invalid-input');
    }

    $("#Senha").on('blur', function () {
        $('.form-group.last').removeClass('invalid-input');
        $('.pwd-error').hide();
    })

    $('.form-control').on('input', function () {
        var $field = $(this).closest('.form-group');
        if (this.value) {
            $field.addClass('field--not-empty');
        } else {
            $field.removeClass('field--not-empty');
        }
    });

    $("#myModal").on('shown.bs.modal', function () {
        $('#NovaSenha').focus();
    });

    if ($('#SenhaExpirada').val() == 'True') {
        var id = $('#Id').val();
        var url = $('.senha-expirada').attr('data-url');
        //console.log(url)
        $('#myModalContent').load(url.replace('_id', id), function () {
            $('#myModal').modal({
                backdrop: 'static',
                keyboard: true
            }, 'show');
            bindForm(this);
        });
        return false;
    }



    $("#frmAtualizarSenha").submit(function (e) {
        e.preventDefault();
        if ($(this).valid()) {
            $.ajax({
                type: "POST",
                url: $(this).attr('action'),
                data: $(this).serialize(),
                success: function (response) {
                    alert(response.message);
                }
            });
        }
    });

    function bindForm(dialog) {
        $('form', dialog).submit(function () {
            $.ajax({
                url: this.action,
                type: this.method,
                data: $(this).serialize(),
                success: function (result) {
                    if (result.success) {
                        $('#SenhaExpirada').val('');
                        $('#myModal').modal('hide');
                        var div = document.createElement("div");
                        div.innerHTML = "<h4 class='swal-text-success'>"+ result.message + "</h4>";

                        swal({
                            title: "Controle de Acesso",
                            content: div,
                            html: true,
                            buttons: {
                                confirm: {
                                    className: "swal-btn-info"
                                }
                            }
                        })

                        //bootbox.alert({
                        //    title: "Controle de Acesso",
                        //    closeButton: false,
                        //    message: result.message,
                        //    callback: function () {
                        //        $('#Senha').focus();
                        //    }
                        //});
                    } else {
                        $('#myModalContent').html(result);
                        bindForm();
                    }
                },
                error: function (jqXhr, textStatus, errorMessage) {
                    var div = document.createElement("div");
                    div.innerHTML = "<h4 class='swal-text-error'>Falha na atualização da senha de acesso!</h4><p class='swal-text-error-detail'>" + response.mensagem + "</p>";
                    swal({
                        title: "Controle de Acesso",
                        content: div,
                        html: true,
                        buttons: {
                            confirm: {
                                visible: false
                            },
                            cancel: {
                                text: "Fechar",
                                className: "swal-btn-danger",
                                closeModal: true,
                                visible: true
                            }
                        }
                    })

                    //bootbox.alert({
                    //    title: "Erro",
                    //    closeButton: false,
                    //    message: result.message,
                    //});
                }
            });
            return false;
        });
    }
});