$(function () {
    'use strict';
    var loginFormSelector = '.login-form';
    var visibleCredentialSelector = '#UsuarioDisplay, #SenhaDisplay';

    function hasValidationMessage($container) {
        return $.trim($container.text()) !== '';
    }

    function syncValidationState($container, $field, forceHide) {
        var showMessage = !forceHide && hasValidationMessage($container);
        $field.toggleClass('invalid-input', showMessage);
        $container.toggleClass('has-message', showMessage);
    }

    function syncFieldState($input) {
        var $field = $input.closest('.form-group');
        if ($field.length === 0) {
            return;
        }

        if (($input.val() || '').trim() !== '') {
            $field.addClass('field--not-empty');
        } else {
            $field.removeClass('field--not-empty');
        }
    }

    function syncAllFieldStates() {
        $('.form-group .form-control').each(function () {
            syncFieldState($(this));
        });
    }

    function syncLoginCredentialPayload() {
        $('#Usuario').val($('#UsuarioDisplay').val() || '');
        $('#Senha').val($('#SenhaDisplay').val() || '');
    }

    function resetLoginCredentials() {
        $('#Usuario, #Senha, #UsuarioDisplay, #SenhaDisplay').val('');
        syncAllFieldStates();
    }

    syncValidationState($('.user-error'), $('.form-group.first'), false);

    $("#UsuarioDisplay").on('blur', function () {
        syncValidationState($('.user-error'), $('.form-group.first'), true);
    });

    syncValidationState($('.pwd-error'), $('.form-group.last'), false);

    $("#SenhaDisplay").on('blur', function () {
        syncValidationState($('.pwd-error'), $('.form-group.last'), true);
    });

    $('.form-control').on('input change blur', function () {
        syncFieldState($(this));
    });

    $(visibleCredentialSelector).on('input change', function () {
        syncLoginCredentialPayload();
    });

    $(loginFormSelector).on('submit', function () {
        syncLoginCredentialPayload();
    });

    resetLoginCredentials();
    syncAllFieldStates();

    $(window).on('load', function () {
        resetLoginCredentials();
    });

    $(window).on('pageshow', function () {
        resetLoginCredentials();
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
    function bindForm(dialog) {
        $('form', dialog).submit(function () {
            $.ajax({
                url: this.action,
                type: this.method,
                data: $(this).serialize(),
                success: function (result) {
                    if (result.success) {
                        resetLoginCredentials();
                        $('#Id').val('');
                        $('#SenhaExpirada').val('');
                        $('#myModalContent').html('');
                        $('#myModal').modal('hide');
                        swal({
                            title: "Controle de Acesso",
                            text: result.message || "Senha atualizada com sucesso.",
                            icon: "success",
                            buttons: {
                                confirm: {
                                    text: "OK",
                                    className: "swal-btn-success",
                                    closeModal: true,
                                    visible: true
                                }
                            }
                        }).then(function () {
                            $('#UsuarioDisplay').focus();
                        });

                    } else {
                        $('#myModalContent').html(result);
                        bindForm();
                    }
                },
                error: function (jqXhr, textStatus, errorMessage) {
                    var response = jqXhr && jqXhr.responseJSON ? jqXhr.responseJSON : {};
                    var detalhe = response.message || errorMessage || 'Falha ao atualizar a senha.';
                    var div = document.createElement("div");
                    div.innerHTML = "<h4 class='swal-text-error'>Falha na atualiza\u00E7\u00E3o da senha de acesso!</h4><p class='swal-text-error-detail'>" + detalhe + "</p>";
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
